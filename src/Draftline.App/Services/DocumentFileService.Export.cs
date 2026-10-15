using Draftline.App.Model;
using Microsoft.JSInterop;

namespace Draftline.App.Services;

public sealed partial class DocumentFileService
{
    private IJSObjectReference? _exportModule;

    /// <summary>Export a group or a multi-selection as one SVG.</summary>
    public async Task DownloadSelectionSvgAsync(DraftDocument document, DraftLayer layer, IEnumerable<string> ids)
    {
        var module=await ModuleAsync(default);
        await module.InvokeVoidAsync("download",FileNameFor(document,"-selection.svg",layer.Name),"image/svg+xml",SvgExporter.ApplyTheme(SvgExporter.ExportSelection(layer,ids),_themes.Drawing(document.MockupTheme)));
    }

    /// <summary>Download one or more screens as PNG files or a linked PDF.</summary>
    public async Task DownloadDrawingAsync(DraftDocument document, IEnumerable<DraftLayer> layers, bool pdf)
    {
        var pages = layers.Select((layer, index) => new
        {
            id = layer.Id, width = layer.CanvasWidth, height = layer.CanvasHeight,
            svg = SvgExporter.ApplyTheme(SvgExporter.Export(layer),_themes.Drawing(document.MockupTheme)), links = ExportLinks.For(layer),
            fileName = FileNameFor(document, $"-{index + 1}.png", layer.Name)
        }).ToArray();
        _exportModule ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/draftline-export.js");
        if (pdf) await _exportModule.InvokeVoidAsync("exportPdf", FileNameFor(document, ".pdf"), pages);
        else await _exportModule.InvokeVoidAsync("exportPng", (object)pages);
    }
}
