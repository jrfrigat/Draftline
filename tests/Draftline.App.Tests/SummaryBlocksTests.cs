using System.Xml.Linq;
using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class SummaryBlocksTests
{
    [Theory]
    [InlineData(WireframeKind.Stat)]
    [InlineData(WireframeKind.PageHeader)]
    public void Authored_summary_content_survives_clone_storage_export_and_history(WireframeKind kind)
    {
        var session = new DocumentSession();
        var block = session.Add(kind, 40, 40);
        session.Edit("Summary", _ =>
        {
            block.Text = "<script>&Metric";
            block.Value = "15 600";
            block.SecondaryText = "<description>&";
            block.StatUnit = "шт.";
            block.StatDelta = "−3%";
            block.StatTone = "warning";
            block.HeaderEyebrow = "<section>";
            block.HeaderAction = "<open>&";
            block.HeadingLevel = 3;
        });
        var clone = block.Clone();
        Assert.Equal(block.StatUnit, clone.StatUnit);
        Assert.Equal(block.StatDelta, clone.StatDelta);
        Assert.Equal(block.StatTone, clone.StatTone);
        Assert.Equal(block.HeaderEyebrow, clone.HeaderEyebrow);
        Assert.Equal(block.HeaderAction, clone.HeaderAction);
        Assert.Equal(block.HeadingLevel, clone.HeadingLevel);

        foreach (var json in new[] { DraftlineJson.Serialize(session.Document), DraftlineJson.SerializeForStorage(session.Document) })
        {
            var restored = DraftlineJson.Read(json).Document!.StartLayer.Elements.Single();
            Assert.Equal(kind, restored.Kind);
            Assert.Equal(block.StatDelta, restored.StatDelta);
            Assert.Equal(block.HeaderAction, restored.HeaderAction);
            Assert.Equal(3, restored.HeadingLevel);
            var svg = XDocument.Parse(SvgExporter.ExportElement(restored));
            var texts = svg.Descendants().Where(n => n.Name.LocalName == "text").Select(n => n.Value).ToList();
            Assert.Contains(block.Text, texts);
            Assert.Contains(block.SecondaryText, texts);
            Assert.Contains(kind == WireframeKind.Stat ? block.Value : block.HeaderAction, texts);
            Assert.Contains(kind == WireframeKind.Stat ? block.StatUnit : block.HeaderEyebrow, texts);
            Assert.DoesNotContain(svg.Descendants(), n => n.Name.LocalName == "script");
            Assert.Contains(svg.Descendants(), n => n.Name.LocalName == "svg" && (string?)n.Attribute("overflow") == "hidden");
        }
        Assert.True(session.Undo());
        Assert.NotEqual("<script>&Metric", session.CurrentLayer.Find(block.Id)!.Text);
        Assert.True(session.Redo());
        Assert.Equal(block.StatTone, session.CurrentLayer.Find(block.Id)!.StatTone);
        Assert.Equal(block.HeaderAction, session.CurrentLayer.Find(block.Id)!.HeaderAction);
    }

    [Fact]
    public void Old_documents_keep_their_blocks_and_missing_summary_fields_take_safe_defaults()
    {
        var result = DraftlineJson.Read("""{"schemaVersion":9,"elements":[{"kind":"Heading","text":"Old heading","width":240,"height":40}]}""");
        Assert.NotNull(result.Document);
        var old = result.Document!.StartLayer.Elements.Single();
        Assert.Equal("Old heading", old.Text);
        Assert.Equal(WireframeKind.Heading, old.Kind);
        Assert.Equal("", old.HeaderAction);
        Assert.Equal(1, old.HeadingLevel);
        var malformed = DraftlineJson.Read("""{"schemaVersion":10,"elements":[{"kind":"Stat","statUnit":null,"statDelta":null,"statTone":"url(bad)","headerEyebrow":null,"headerAction":null,"headingLevel":99}]}""");
        Assert.NotNull(malformed.Document);
        var block = malformed.Document!.StartLayer.Elements.Single();
        Assert.Equal("", block.StatUnit);
        Assert.Equal("", block.StatDelta);
        Assert.Equal("default", block.StatTone);
        Assert.Equal("", block.HeaderEyebrow);
        Assert.Equal("", block.HeaderAction);
        Assert.Equal(6, block.HeadingLevel);
    }

    [Fact]
    public void Page_header_preview_action_uses_existing_document_navigation()
    {
        var session = new DocumentSession();
        var header = session.Add(WireframeKind.PageHeader, 0, 0);
        var next = session.AddLayer();
        header.TargetLayerId = next.Id;
        header.Transition = TransitionKind.Navigate;
        var before = DraftlineJson.Serialize(session.Document);
        session.EnterPreview();
        Assert.True(session.FollowTransition(header));
        Assert.Equal(next.Id, session.PreviewLayerId);
        session.ExitPreview();
        Assert.Equal(before, DraftlineJson.Serialize(session.Document));
    }

    [Fact]
    public void PDF_link_covers_only_the_visible_header_action_and_disappears_with_it()
    {
        var layer = DraftLayer.Create("Page");
        var header = ElementDefaults.Create(WireframeKind.PageHeader);
        header.X = 40; header.Y = 48;
        header.TargetLayerId = "next";
        header.Transition = TransitionKind.Navigate;
        layer.Elements.Add(header);
        var link = Assert.Single(ExportLinks.For(layer));
        Assert.Equal(SummaryBlocks.ActionBounds(header), new Rect(link.X, link.Y, link.Width, link.Height));
        header.HeaderAction = "";
        Assert.Empty(ExportLinks.For(layer));
        header.HeaderAction = "Open";
        header.Disabled = true;
        Assert.Empty(ExportLinks.For(layer));
        header.Disabled = false;
        header.Height = 80;
        Assert.Empty(ExportLinks.For(layer));
    }
}
