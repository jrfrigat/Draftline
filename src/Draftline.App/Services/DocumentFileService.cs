using System.Text;
using Draftline.App.Model;
using Draftline.App.Theming;
using Microsoft.JSInterop;

namespace Draftline.App.Services;
/// <summary>
/// Handing a wireframe to the person as a file, and taking one back.
///
/// Both directions need the browser: a download is a Blob behind an anchor, and choosing a file is an
/// input the person has to click. What stays on this side is the part that can be reasoned about - what
/// the file is called, what goes into it, and whether what came back is a wireframe at all.
/// </summary>
public sealed partial class DocumentFileService : IAsyncDisposable
{
    private const string ModulePath = "./js/draftline-files.js";
    private readonly IJSRuntime _js;
    private readonly ThemeCoordinator _themes;
    private IJSObjectReference? _module;
    /// <summary>The file operations over the given JavaScript runtime.</summary>
    public DocumentFileService(IJSRuntime js, ThemeCoordinator themes)
    {
        _js = js;
        _themes = themes;
    }

    /// <summary>Hands the open wireframe to the person as a JSON file.</summary>
    public async Task DownloadJsonAsync(DraftDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync("download", cancellationToken, FileNameFor(document, DraftlineJson.FileExtension), "application/json", DraftlineJson.Serialize(document)).ConfigureAwait(false);
    }

    /// <summary>Hands one screen to the person as an SVG drawing - the whole sheet, or one block.</summary>
    public async Task DownloadSvgAsync(DraftDocument document, DraftLayer layer, WireframeElement? element = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(layer);
        var drawing = element is null ? SvgExporter.Export(layer) : SvgExporter.ExportElement(element, layer);
        drawing = SvgExporter.ApplyTheme(drawing, _themes.Drawing(document.MockupTheme));
        var suffix = element is null ? string.Empty : "-" + element.Kind.ToString().ToLowerInvariant();
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        await module.InvokeVoidAsync("download", cancellationToken, FileNameFor(document, suffix + ".svg", layer.Name), "image/svg+xml", drawing).ConfigureAwait(false);
    }

    /// <summary>
    /// Hands every screen to the person in one ZIP, avoiding blocked multiple downloads.
    /// </summary>
    public async Task DownloadAllSvgAsync(DraftDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        var files = SvgExporter.ExportAll(document).Select((entry, index) => new { name = $"{index + 1}-" + FileNameFor(document, ".svg", entry.Name), text = SvgExporter.ApplyTheme(entry.Svg, _themes.Drawing(document.MockupTheme)) }).ToArray();
        await module.InvokeVoidAsync("downloadTextZip", cancellationToken, FileNameFor(document, "-svg.zip"), (object)files);
    }

    /// <summary>
    /// Asks the person for a file and reads it as a wireframe. Null means they changed their mind, which
    /// is not a mistake and must not be reported as one.
    /// </summary>
    public Task<DocumentReadResult?> ImportAsync(CancellationToken cancellationToken = default) =>
        ImportAsync(_ => Task.CompletedTask, cancellationToken);

    /// <summary>Transport a selected Blob in byte chunks and report actual reading progress.</summary>
    public async Task<DocumentReadResult?> ImportAsync(Func<DocumentImportProgress, Task> progress, CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        if (!await module.InvokeAsync<bool>("pickJsonFile", cancellationToken).ConfigureAwait(false)) return null;
        var file = await module.InvokeAsync<IJSStreamReference>("takePickedFile", cancellationToken).ConfigureAwait(false);
        await using (file)
        {
            const long maximumBytes = 128 * 1024 * 1024;
            if (file.Length > maximumBytes)
                return DocumentReadResult.Failed("Файл больше 128 МБ. Разделите макет на несколько проектов.");
            await using var stream = await file.OpenReadStreamAsync(maximumBytes, cancellationToken);
            return await DraftlineJson.ReadAsync(stream, file.Length, progress, cancellationToken);
        }
    }

    /// <summary>Allow the rendered import status to reach the browser before synchronous model work.</summary>
    public async Task PaintImportProgressAsync() =>
        await (await ModuleAsync(CancellationToken.None)).InvokeVoidAsync("paintImportProgress");

    /// <summary>
    /// What the file is called: the wireframe's own name, and the screen's when the file is one screen,
    /// with everything a file system would rather not see replaced by a dash - so two people exporting
    /// the same wireframe get the same file names.
    /// </summary>
    public static string FileNameFor(DraftDocument document, string extension, string? layerName = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return string.IsNullOrWhiteSpace(layerName) ? Slug(document.Name) + extension : Slug(document.Name) + "-" + Slug(layerName) + extension;
    }

    /// <summary>A name that is safe everywhere: letters, digits, single dashes between them.</summary>
    private static string Slug(string name)
    {
        var builder = new StringBuilder();
        foreach (var character in name.Trim())
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '-');
        }

        var slug = builder.ToString().Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrEmpty(slug) ? "draftline" : slug;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_exportModule is not null)
        {
            await _exportModule.DisposeAsync();
            _exportModule = null;
        }

        if (_module is not null)
        {
            await _module.DisposeAsync().ConfigureAwait(false);
            _module = null;
        }
    }

    private async Task<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken) => _module ??= await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath).ConfigureAwait(false);
}
