using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using Draftline.App.Model;

namespace Draftline.App.Services;
/// <summary>
/// The JSON contract of a wireframe file, as a source-generated context.
///
/// Generated rather than reflected on purpose: the editor runs in WebAssembly, where the trimmer
/// removes every member it cannot see being used, and reflection is exactly the pattern it cannot
/// see. This context is therefore the list of types a file may contain - one place to change when the
/// document grows a field.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true, IgnoreReadOnlyProperties = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(DraftDocument))]
[JsonSerializable(typeof(WireframeElement))]
public sealed partial class DraftlineJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Reading and writing a wireframe as a file.
///
/// Nothing here throws at its caller. A file that is not a Draftline wireframe is an ordinary outcome
/// of asking someone for a file, and the editor answers it with a sentence rather than with a stack
/// trace - which is the difference between "этот файл не подходит" and a broken screen.
/// </summary>
public static class DraftlineJson
{
    /// <summary>What a saved wireframe is called on disk.</summary>
    public const string FileExtension = ".wireframe.json";
    /// <summary>
    /// Writes a document as JSON. The copy is normalized first, so a file that goes out is always a
    /// canonical one: identities filled in, blocks inside the sheet, schema version stated.
    /// </summary>
    public static string Serialize(DraftDocument document, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(document);
        var canonical = document.Clone();
        canonical.Normalize();
        return JsonSerializer.Serialize(canonical, indented ? ExportType : StorageType);
    }

    private static readonly JsonTypeInfo<DraftDocument> StorageType = WriteType(false);
    private static readonly JsonTypeInfo<DraftDocument> ExportType = WriteType(true);
    private static JsonTypeInfo<DraftDocument> WriteType(bool indented)
    {
        var options = new JsonSerializerOptions(DraftlineJsonContext.Default.Options)
        {
            WriteIndented = indented,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = DraftlineJsonContext.Default.WithAddedModifier(info =>
            {
                if (info.Type != typeof(WireframeElement)) return;
                var defaults = new WireframeElement();
                foreach (var property in info.Properties)
                {
                    var baseline = property.Get?.Invoke(defaults);
                    // Compare with constructor defaults, not CLR zero: SelectedIndex defaults to -1,
                    // so dropping an explicit zero would silently lose the selected option.
                    property.ShouldSerialize = (_, value) => !Equals(value, baseline) &&
                        !(value is System.Collections.ICollection { Count: 0 } && baseline is System.Collections.ICollection { Count: 0 });
                }
            })
        };
        return (JsonTypeInfo<DraftDocument>)options.GetTypeInfo(typeof(DraftDocument));
    }
    /// <summary>Writes the trusted in-memory document without repeating import validation and decoding images.</summary>
    public static string SerializeForStorage(DraftDocument document) => JsonSerializer.Serialize(document, StorageType);
    /// <summary>Reads a document, or explains why it could not be read.</summary>
    public static DocumentReadResult Read(string json)
    {
        try
        {
            var result = Decode(Encoding.UTF8.GetBytes(json));
            result.Document?.Normalize();
            return result;
        }
        catch (Exception)
        {
            return DocumentReadResult.Failed("Не удалось прочитать макет. Выберите другой JSON-файл Draftline.");
        }
    }

    /// <summary>Read bytes without a full UTF16 string or a second JSON DOM in WebAssembly.</summary>
    public static async Task<DocumentReadResult> ReadAsync(Stream stream, long length,
        Func<DocumentImportProgress, Task> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            using var buffer = new MemoryStream(checked((int)length));
            var chunk = new byte[64 * 1024];
            int count;
            await progress(new("Чтение файла", 0, length));
            while ((count = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
                await progress(new("Чтение файла", buffer.Length, length));
            }
            await progress(new("Проверка JSON"));
            var result = Decode(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
            if (result.Document is { } document)
            {
                await progress(new("Подготовка экранов", 0, document.Layers.Count));
                await document.NormalizeAsync((done, total) => progress(new("Подготовка экранов", done, total)));
            }
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception)
        {
            return DocumentReadResult.Failed("Не удалось прочитать макет. Выберите другой JSON-файл Draftline.");
        }
    }

    private static DocumentReadResult Decode(ReadOnlySpan<byte> json)
    {
        // UTF8 BOM is legal in a file, but Utf8JsonReader expects the first JSON token.
        if (json.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) json = json[3..];
        if (json.IsEmpty) return DocumentReadResult.Failed("Файл пуст.");
        try
        {
            if (!LooksLikeAWireframe(json))
                return DocumentReadResult.Failed("В файле нет описания макета Draftline.");
        }
        catch (JsonException)
        {
            return DocumentReadResult.Failed("В файле некорректный JSON. Выберите файл макета Draftline.");
        }

        DraftDocument? document;
        try
        {
            document = JsonSerializer.Deserialize(json, DraftlineJsonContext.Default.DraftDocument);
        }
        catch (JsonException)
        {
            return DocumentReadResult.Failed("Данные файла не соответствуют формату макета Draftline.");
        }

        if (document is null)
        {
            return DocumentReadResult.Failed("В файле нет макета.");
        }

        // The version is checked before anything else is believed: a file from a newer Draftline may
        // mean something else by the same field name, and reading it as if it meant the same thing
        // would quietly damage it.
        if (document.SchemaVersion > DraftDocument.CurrentSchemaVersion)
        {
            return DocumentReadResult.Failed($"Файл создан более новой версией Draftline (схема {document.SchemaVersion}, эта версия читает {DraftDocument.CurrentSchemaVersion}).");
        }

        return DocumentReadResult.Ok(document);
    }

    /// <summary>True when the JSON object carries at least one field a wireframe has.</summary>
    private static bool LooksLikeAWireframe(ReadOnlySpan<byte> json)
    {
        var reader = new Utf8JsonReader(json);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject) return false;
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var name = reader.GetString();
            if (name is not null && (name.Equals("schemaVersion", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("elements", StringComparison.OrdinalIgnoreCase) || name.Equals("layers", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("canvasWidth", StringComparison.OrdinalIgnoreCase)))
                return true;
            reader.Read();
            reader.Skip();
        }
        return false;
    }
}

/// <summary>What reading a file produced: the document, or the sentence that says why there is none.</summary>
/// <param name = "Document">The document, when reading succeeded.</param>
/// <param name = "Error">Why there is no document, when reading failed.</param>
public sealed record DocumentReadResult(DraftDocument? Document, string? Error)
{
    /// <summary>True when a document came back.</summary>
    public bool Succeeded => Document is not null;

    /// <summary>A document was read.</summary>
    public static DocumentReadResult Ok(DraftDocument document) => new(document, null);
    /// <summary>No document, and the reason why.</summary>
    public static DocumentReadResult Failed(string error) => new(null, error);
}
