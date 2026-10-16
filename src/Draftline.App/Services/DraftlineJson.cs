using System.Text.Json;
using System.Text.Json.Serialization;
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
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
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

        var context = indented ? DraftlineJsonContext.Default : CompactContext;
        return JsonSerializer.Serialize(canonical, context.DraftDocument);
    }

    private static readonly DraftlineJsonContext CompactContext = new(new JsonSerializerOptions(DraftlineJsonContext.Default.Options) { WriteIndented = false });

    /// <summary>Reads a document, or explains why it could not be read.</summary>
    public static DocumentReadResult Read(string json)
    {
        try { return ReadCore(json); }
        catch (Exception exception)
        {
            return DocumentReadResult.Failed("Не удалось прочитать макет: " + exception.Message);
        }
    }

    private static DocumentReadResult ReadCore(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return DocumentReadResult.Failed("Файл пуст.");
        }

        JsonDocument parsed;
        try
        {
            parsed = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            return DocumentReadResult.Failed("Это не макет Draftline: " + exception.Message);
        }

        using (parsed)
        {
            // A document with no recognizable field is not a wireframe that happens to be empty - it
            // is another kind of file entirely, and saying so is more useful than opening an empty
            // sheet and letting the person wonder where their work went.
            if (!LooksLikeAWireframe(parsed.RootElement))
            {
                return DocumentReadResult.Failed("В файле нет описания макета Draftline.");
            }
        }

        DraftDocument? document;
        try
        {
            document = JsonSerializer.Deserialize(json, DraftlineJsonContext.Default.DraftDocument);
        }
        catch (JsonException exception)
        {
            return DocumentReadResult.Failed("Это не макет Draftline: " + exception.Message);
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
            return DocumentReadResult.Failed(
                $"Файл создан более новой версией Draftline (схема {document.SchemaVersion}, эта версия читает {DraftDocument.CurrentSchemaVersion}).");
        }

        document.Normalize();
        return DocumentReadResult.Ok(document);
    }

    /// <summary>True when the JSON object carries at least one field a wireframe has.</summary>
    private static bool LooksLikeAWireframe(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals("schemaVersion", StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals("elements", StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals("canvasWidth", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>What reading a file produced: the document, or the sentence that says why there is none.</summary>
/// <param name="Document">The document, when reading succeeded.</param>
/// <param name="Error">Why there is no document, when reading failed.</param>
public sealed record DocumentReadResult(DraftDocument? Document, string? Error)
{
    /// <summary>True when a document came back.</summary>
    public bool Succeeded => Document is not null;

    /// <summary>A document was read.</summary>
    public static DocumentReadResult Ok(DraftDocument document) => new(document, null);

    /// <summary>No document, and the reason why.</summary>
    public static DocumentReadResult Failed(string error) => new(null, error);
}
