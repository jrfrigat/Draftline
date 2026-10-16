using System.Text.Json;
using System.Text.Json.Serialization;
using Draftline.App.Model;

namespace Draftline.App.Services;

/// <summary>One line of the project list: what the shelf can show without opening anything.</summary>
public sealed class ProjectSummary
{
    /// <summary>The document's identity.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>What the person called it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>When it was last written.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>How many blocks it holds - a size the list can show at a glance.</summary>
    public int ElementCount { get; set; }
}

/// <summary>The JSON contract of the project list itself, separate from the wireframe's own format.</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(List<ProjectSummary>))]
public sealed partial class ProjectCatalogJsonContext : JsonSerializerContext
{
}

/// <summary>
/// The wireframes this browser holds, and the way to read and write each of them.
///
/// It is the only place that knows how the storage is laid out - one key for the list, one key per
/// document, one key remembering which was open - so a change of storage (IndexedDB, or a file the
/// person keeps) is a change to this class and to nothing else.
/// </summary>
public sealed class ProjectCatalog
{
    /// <summary>Key the list itself lives under.</summary>
    public const string IndexKey = "draftline.index";

    /// <summary>Prefix of a document's key; the document's identity follows it.</summary>
    public const string DocumentKeyPrefix = "draftline.project.";

    /// <summary>Key holding the identity of the wireframe that was open last.</summary>
    public const string LastOpenKey = "draftline.last";

    private readonly IBrowserStorage _storage;

    /// <summary>The catalog over a storage.</summary>
    public ProjectCatalog(IBrowserStorage storage) => _storage = storage;

    /// <summary>An imported file never overwrites an existing project without consent.</summary>
    public async Task<DraftDocument> PrepareImportAsync(DraftDocument source)
    {
        var document = source.Clone();
        if (await _storage.GetAsync(DocumentKeyPrefix + document.Id) is not null ||
            (await ListAsync()).Any(line => line.Id == document.Id))
        {
            do { document.Id = Guid.NewGuid().ToString("n"); }
            while (await _storage.GetAsync(DocumentKeyPrefix + document.Id) is not null);
            document.Name += " (копия)";
        }
        return document;
    }

    /// <summary>The projects, most recently written first.</summary>
    public async Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var json = await _storage.GetAsync(IndexKey, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<ProjectSummary>();
        }

        try
        {
            var list = JsonSerializer.Deserialize(json, ProjectCatalogJsonContext.Default.ListProjectSummary);

            return list is null
                ? Array.Empty<ProjectSummary>()
                : list.Where(line => line is not null && !string.IsNullOrWhiteSpace(line.Id))
                    .OrderByDescending(line => line.UpdatedAt)
                    .ToList();
        }
        catch (JsonException)
        {
            // A list that cannot be read is not worth losing the app over. The documents themselves
            // are still in the storage, so the list starts empty and refills as they are saved.
            return Array.Empty<ProjectSummary>();
        }
    }

    /// <summary>Writes a wireframe and brings its line in the list up to date.</summary>
    public async Task SaveAsync(DraftDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await _storage.SetAsync(DocumentKeyPrefix + document.Id, DraftlineJson.SerializeForStorage(document), cancellationToken)
            .ConfigureAwait(false);

        var list = (await ListAsync(cancellationToken).ConfigureAwait(false)).ToList();
        var line = list.FirstOrDefault(candidate => candidate.Id == document.Id);

        if (line is null)
        {
            line = new ProjectSummary { Id = document.Id };
            list.Add(line);
        }

        line.Name = document.Name;
        line.UpdatedAt = document.UpdatedAt;
        line.ElementCount = document.TotalElements;

        await _storage.SetAsync(
                IndexKey,
                JsonSerializer.Serialize(list, ProjectCatalogJsonContext.Default.ListProjectSummary),
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Reads a wireframe back; the result carries the reason when it cannot be read.</summary>
    public async Task<DocumentReadResult> LoadAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return DocumentReadResult.Failed("Проект не выбран.");
        }

        var json = await _storage.GetAsync(DocumentKeyPrefix + id, cancellationToken).ConfigureAwait(false);
        if (json is null)
        {
            return DocumentReadResult.Failed("Этот макет не найден в хранилище браузера.");
        }

        var result = DraftlineJson.Read(json);
        var document = result.Document;

        // A document that was copied and renamed on the way in still belongs under the key it came
        // from, otherwise saving it back would leave a second copy behind.
        if (document is not null && document.Id != id)
        {
            document.Id = id;
        }

        return result;
    }

    /// <summary>Forgets a wireframe, and the fact that it was the last one open.</summary>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _storage.RemoveAsync(DocumentKeyPrefix + id, cancellationToken).ConfigureAwait(false);

        var list = (await ListAsync(cancellationToken).ConfigureAwait(false))
            .Where(line => line.Id != id)
            .ToList();

        await _storage.SetAsync(
                IndexKey,
                JsonSerializer.Serialize(list, ProjectCatalogJsonContext.Default.ListProjectSummary),
                cancellationToken)
            .ConfigureAwait(false);

        if (await LastOpenIdAsync(cancellationToken).ConfigureAwait(false) == id)
        {
            await _storage.RemoveAsync(LastOpenKey, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>The wireframe that was open the last time, if there was one.</summary>
    public Task<string?> LastOpenIdAsync(CancellationToken cancellationToken = default) =>
        _storage.GetAsync(LastOpenKey, cancellationToken);

    /// <summary>Remembers which wireframe is open, so the next visit opens it again.</summary>
    public Task RememberLastAsync(string id, CancellationToken cancellationToken = default) =>
        _storage.SetAsync(LastOpenKey, id, cancellationToken);

    /// <summary>A name no existing wireframe uses: "Макет 1", "Макет 2", and so on.</summary>
    public async Task<string> SuggestNameAsync(CancellationToken cancellationToken = default)
    {
        var list = await ListAsync(cancellationToken).ConfigureAwait(false);

        for (var index = 1; ; index++)
        {
            var name = "Макет " + index;
            if (list.All(line => !string.Equals(line.Name, name, StringComparison.Ordinal)))
            {
                return name;
            }
        }
    }
}
