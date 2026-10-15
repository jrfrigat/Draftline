using System.Text;
using Draftline.App.Model;
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
    private IJSObjectReference? _module;

    /// <summary>The file operations over the given JavaScript runtime.</summary>
    public DocumentFileService(IJSRuntime js) => _js = js;

    /// <summary>Hands the open wireframe to the person as a JSON file.</summary>
    public async Task DownloadJsonAsync(DraftDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);

        await module.InvokeVoidAsync(
                "download",
                cancellationToken,
                FileNameFor(document, DraftlineJson.FileExtension),
                "application/json",
                DraftlineJson.Serialize(document))
            .ConfigureAwait(false);
    }

    /// <summary>Hands one screen to the person as an SVG drawing - the whole sheet, or one block.</summary>
    public async Task DownloadSvgAsync(
        DraftDocument document,
        DraftLayer layer,
        WireframeElement? element = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(layer);

        var drawing = element is null ? SvgExporter.Export(layer) : SvgExporter.ExportElement(element, layer);
        var suffix = element is null ? string.Empty : "-" + element.Kind.ToString().ToLowerInvariant();

        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);

        await module.InvokeVoidAsync(
                "download",
                cancellationToken,
                FileNameFor(document, suffix + ".svg", layer.Name),
                "image/svg+xml",
                drawing)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Hands every screen to the person, one file each. A wireframe of five screens is five pictures,
    /// and one file carrying all of them would be a format nobody could open anywhere.
    /// </summary>
    public async Task DownloadAllSvgAsync(DraftDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (name, svg) in SvgExporter.ExportAll(document))
        {
            await module.InvokeVoidAsync(
                    "download",
                    cancellationToken,
                    FileNameFor(document, ".svg", name),
                    "image/svg+xml",
                    svg)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Asks the person for a file and reads it as a wireframe. Null means they changed their mind, which
    /// is not a mistake and must not be reported as one.
    /// </summary>
    public async Task<DocumentReadResult?> ImportAsync(CancellationToken cancellationToken = default)
    {
        var module = await ModuleAsync(cancellationToken).ConfigureAwait(false);
        var text = await module.InvokeAsync<string?>("pickTextFile", cancellationToken, ".json").ConfigureAwait(false);

        return text is null ? null : DraftlineJson.Read(text);
    }

    /// <summary>
    /// What the file is called: the wireframe's own name, and the screen's when the file is one screen,
    /// with everything a file system would rather not see replaced by a dash - so two people exporting
    /// the same wireframe get the same file names.
    /// </summary>
    public static string FileNameFor(DraftDocument document, string extension, string? layerName = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        return string.IsNullOrWhiteSpace(layerName)
            ? Slug(document.Name) + extension
            : Slug(document.Name) + "-" + Slug(layerName) + extension;
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_exportModule is not null) { await _exportModule.DisposeAsync(); _exportModule = null; }
        if (_module is not null)
        {
            await _module.DisposeAsync().ConfigureAwait(false);
            _module = null;
        }
    }

    private async Task<IJSObjectReference> ModuleAsync(CancellationToken cancellationToken) =>
        _module ??= await _js.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath)
            .ConfigureAwait(false);
}
