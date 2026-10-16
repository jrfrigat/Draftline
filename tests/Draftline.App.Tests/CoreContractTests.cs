using System.Xml.Linq;
using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class CoreContractTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public void All_supported_schemas_roundtrip_to_the_current_contract(int version)
    {
        var json = $$"""{"schemaVersion":{{version}},"canvasWidth":800,"canvasHeight":600,"elements":[{"kind":3,"text":"Legacy","width":120,"height":40}]}""";
        var result = DraftlineJson.Read(json);
        Assert.True(result.Succeeded, result.Error);
        Assert.Equal(DraftDocument.CurrentSchemaVersion, result.Document!.SchemaVersion);
        Assert.Single(result.Document.StartLayer.Elements);
        Assert.Equal("Legacy", DraftlineJson.Read(DraftlineJson.Serialize(result.Document)).Document!.StartLayer.Elements[0].Text);
    }

    [Fact]
    public void History_snapshots_are_isolated_and_new_edits_abandon_redo()
    {
        var history = new DocumentHistory(2); var document = DraftDocument.CreateNew("До");
        history.Record(document, "Имя"); document.Name = "После";
        var undo = history.Undo(document)!;
        Assert.Equal("До", undo.Document.Name);
        undo.Document.Name = "Изменённый результат";
        Assert.Equal("После", history.Redo(undo.Document)!.Document.Name);
        history.Undo(document); history.Record(document, "Новая ветка"); Assert.False(history.CanRedo);
    }

    [Fact]
    public void SVG_escapes_user_text_as_text()
    {
        var element = ElementDefaults.Create(WireframeKind.Button); element.Text = "<script>&\"'";
        var svg = XDocument.Parse(SvgExporter.ExportElement(element));
        Assert.Contains(svg.Descendants(), node => node.Name.LocalName == "text" && node.Value == element.Text);
        Assert.DoesNotContain(svg.Descendants(), node => node.Name.LocalName == "script");
    }

    [Fact]
    public void Geometry_snaps_clamps_and_refuses_overlapping_palette_placement()
    {
        Assert.Equal(16, CanvasGeometry.Snap(12, 8));
        Assert.Equal(new Rect(0, 60, 120, 40), CanvasGeometry.Clamp(new Rect(-10, 200, 120, 40), 200, 100));
        Assert.Null(CanvasGeometry.FreeSpace(new Rect(0, 0, 120, 40), 120, 40, [new Rect(0, 0, 120, 40)], 8));
    }

    [Fact]
    public async Task Catalog_ignores_null_index_rows_and_preserves_documents()
    {
        var storage = new InMemoryBrowserStorage(); var catalog = new ProjectCatalog(storage);
        var document = DraftDocument.CreateNew("Сохранённый");
        await catalog.SaveAsync(document, TestContext.Current.CancellationToken);
        await storage.SetAsync(ProjectCatalog.IndexKey, "[null]", TestContext.Current.CancellationToken);
        Assert.Empty(await catalog.ListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(document.Name, (await catalog.LoadAsync(document.Id, TestContext.Current.CancellationToken)).Document!.Name);
    }
}
