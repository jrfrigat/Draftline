using System.Globalization;
using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;

/// <summary>
/// Writes a wireframe as an SVG drawing.
///
/// It is a generator rather than a screenshot: the same low-fidelity language the canvas draws - ruled
/// rectangles, a cross where a picture goes, lines for a list - expressed as vector shapes. Asking the
/// browser what it rendered would give a picture of one screen at one zoom level; this gives the
/// document, and the document is what the person means to hand over.
///
/// The drawing is honest about its colours, too: a wireframe is black on white wherever it is opened,
/// because the sheet is the document's paper and not a part of anybody's theme.
/// </summary>
public static partial class SvgExporter
{
    private const string Ink = "#1b1e24";
    private const string Paper = "#ffffff";
    private const string Font = "Inter, Segoe UI, system-ui, sans-serif";

    /// <summary>The whole sheet of one screen as an SVG drawing.</summary>
    public static string Export(DraftLayer layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        var width = Math.Max(1, layer.CanvasWidth);
        var height = Math.Max(1, layer.CanvasHeight);
        var svg = new StringBuilder();

        Start(svg, width, height);
        foreach (var element in TabPanels.Visible(layer).OrderBy(element =>
                     (element.Kind is WireframeKind.Select or WireframeKind.MenuBar) && ElementState.IsExpanded(element) ? 1 : 0))
        {
            WriteElement(svg, element);
        }

        End(svg);
        return svg.ToString();
    }

    /// <summary>Every screen of a document as a drawing of its own, in the order the screens are listed.</summary>
    public static IReadOnlyList<(string Name, string Svg)> ExportAll(DraftDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return document.Layers
            .Select(layer => (layer.Name, Export(layer)))
            .ToList();
    }

    /// <summary>One block on a sheet of its own - what a person wants when they export a single control.</summary>
    public static string ExportElement(WireframeElement element, DraftLayer? layer = null)
    {
        ArgumentNullException.ThrowIfNull(element);

        var width = Math.Max(1, element.Width);
        var height = Math.Max(1, ElementState.DrawingHeight(element));
        var svg = new StringBuilder();

        Start(svg, width, height);
        WriteElement(svg, element, offsetX: -element.X, offsetY: -element.Y);
        if (layer is not null && TabPanels.IsContainer(element.Kind))
        {
            foreach (var child in TabPanels.Visible(layer).Where(child => child.ParentTabsId == element.Id))
                WriteElement(svg, child, offsetX: -element.X, offsetY: -element.Y);
        }
        End(svg);

        return svg.ToString();
    }

    /// <summary>Draws one block. Every kind is a few shapes - the same language the canvas speaks.</summary>
    private static void WriteElement(StringBuilder svg, WireframeElement element, double offsetX = 0, double offsetY = 0)
    {
        var x = element.X + offsetX;
        var y = element.Y + offsetY;
        var right = x + element.Width;
        var bottom = y + element.Height;
        var middle = x + (element.Width / 2);
        var centerY = y + (element.Height / 2);
        var text = element.DisplayText;
        var size = Math.Max(1, Math.Round(12 * element.FontScale, 1));
        var weight = element.Bold ? "700" : "400";
        var checkTop = y + ((element.Height - 14) / 2);

        // One group per block: the drawing stays readable in a vector editor, and anything reading the
        // file afterwards - a check, a diff, a person - gets the block as one thing.
        svg.Append(element.Disabled ? "<g opacity=\"0.45\">" : "<g>");

        switch (element.Kind)
        {
            case WireframeKind.Panel:
                Box(svg, x, y, element.Width, element.Height, "none");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Text(svg, x + 6, y + 14, text.ToUpperInvariant(), 11, "400", 0.7);
                }

                if (!string.IsNullOrWhiteSpace(element.SecondaryText))
                {
                    Text(svg, x + 6, y + 28, element.SecondaryText, 10, "400", 0.65);
                }

                break;

            case WireframeKind.Heading:
                Text(svg, x, centerY + (size / 3), text, Math.Max(size, 20), "700", 1);
                break;

            case WireframeKind.Text:
                WriteLines(svg, text, x + 2, y + size, size, element.Height);

                if (!string.IsNullOrWhiteSpace(element.SecondaryText))
                {
                    Text(svg, x + 2, y + size + (size * 1.35), element.SecondaryText, size * 0.9, "400", 0.7);
                }

                break;

            case WireframeKind.Button:
                Box(svg, x, y, element.Width, element.Height, element.VisualState == ControlVisualState.Selected ? "#e5e7eb" : Paper);
                Text(svg, middle, centerY + (size / 3), text, size, weight, 1, "middle");
                break;

            case WireframeKind.TextField:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, x + 6, centerY + (size / 3), text, size, weight, 1);
                Text(svg, right - 12, centerY + (size / 3), element.DisplayValue, size, "400",
                    element.ShowsHint ? 0.55 : 1, "end");
                Line(svg, right - 6, centerY - (element.Height * 0.3), right - 6, centerY + (element.Height * 0.3), 1);
                break;

            case WireframeKind.TextArea:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, x + 6, y + size + 2, text, size, weight, 1);

                if (!string.IsNullOrWhiteSpace(element.Value))
                {
                    WriteLines(svg, element.Value, x + 6, y + size + 2 + (size * 1.35), size, element.Height);
                }

                // Ruled lines: what a person draws in a box on paper to say "there is more room here".
                for (var lineY = y + 37; lineY < bottom - 4; lineY += 16)
                {
                    Line(svg, x + 6, lineY, right - 6, lineY, 0.35);
                }

                break;

            case WireframeKind.Checkbox:
                Box(svg, x, checkTop, 14, 14, Paper);
                if (element.Indeterminate)
                    Line(svg, x + 3, checkTop + 7, x + 11, checkTop + 7, 1);
                else if (element.Checked)
                {
                    Line(svg, x + 3, checkTop + 7, x + 6, checkTop + 11, 1);
                    Line(svg, x + 6, checkTop + 11, x + 11, checkTop + 3, 1);
                }

                Text(svg, x + 20, centerY + (size / 3), text, size, weight, 1);
                break;

            case WireframeKind.Radio:
                Circle(svg, x + 7, centerY, 7, Paper);
                if (element.Checked)
                {
                    Circle(svg, x + 7, centerY, 3.5, Ink);
                }

                Text(svg, x + 20, centerY + (size / 3), text, size, weight, 1);
                break;

            case WireframeKind.Select:
            case WireframeKind.List:
            case WireframeKind.MenuBar:
            case WireframeKind.Breadcrumbs:
            case WireframeKind.StatusBar:
                WriteChoices(svg, element, x, y, size);
                break;

            case WireframeKind.Image:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Line(svg, x, y, right, bottom, 1);
                Line(svg, x, bottom, right, y, 1);
                if (!string.IsNullOrWhiteSpace(element.Text))
                {
                    Text(svg, middle, bottom - 6, element.Text, 11, weight, 0.85, "middle");
                }

                break;

            case WireframeKind.Divider:
                if (element.Vertical) Line(svg, middle, y, middle, bottom, 1);
                else Line(svg, x, centerY, right, centerY, 1);
                break;

            case WireframeKind.DataGrid:
                WriteTable(svg, element, x, y, size);
                break;

            case WireframeKind.Tabs:
            {
                var tabs = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var tabWidth = element.Width / Math.Max(1, tabs.Count);
                var tabHeight = Math.Min(TabPanels.HeaderHeight, element.Height);
                Box(svg, x, y, element.Width, element.Height, Paper);

                for (var tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
                {
                    var tabLeft = x + (tabWidth * tabIndex);
                    var (label, _) = ElementDefaults.SplitOption(tabs[tabIndex]);

                    Text(svg, tabLeft + (tabWidth / 2), y + tabHeight / 2 + (size / 3), label, size,
                        tabIndex == ElementState.Index(element) ? "700" : "400", tabIndex == ElementState.Index(element) ? 1 : 0.7, "middle");
                    if (tabIndex == ElementState.Index(element))
                        Line(svg, tabLeft, y + tabHeight - 2, tabLeft + tabWidth, y + tabHeight - 2, 2);
                    Line(svg, tabLeft, y, tabLeft, y + tabHeight, 1);
                }

                Line(svg, x, y + tabHeight, right, y + tabHeight, 1);
                WriteLines(svg, TabPanels.Content(element), x + 8, y + tabHeight + size + 8, size,
                    bottom - 8);
                break;
            }

            case WireframeKind.Accordion:
            {
                var sections = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var sectionHeight = 28d;
                var sectionTop = y;

                for (var sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
                {
                    var (label, _) = ElementDefaults.SplitOption(sections[sectionIndex]);
                    var openSection = ElementState.IsExpanded(element) && sectionIndex == Math.Max(0, ElementState.Index(element));

                    Text(svg, x + 8, sectionTop + (sectionHeight / 2) + 4, label, size,
                        openSection ? "700" : "400", 1);
                    Text(svg, right - 16, sectionTop + (sectionHeight / 2) + 4, openSection ? "−" : "+", size, "400", 0.8);

                    if (sectionIndex > 0)
                    {
                        Line(svg, x, sectionTop, right, sectionTop, 1);
                    }
                    sectionTop += sectionHeight;
                    if (openSection)
                    {
                        var bodyHeight = TabPanels.PanelBounds(element).Height;
                        WriteLines(svg, TabPanels.Content(element), x + 8, sectionTop + size + 8,
                            size, sectionTop + bodyHeight - 4);
                        sectionTop += bodyHeight;
                    }
                }

                Box(svg, x, y, element.Width, element.Height, "none");
                break;
            }

            case WireframeKind.Tree:
            {
                var rowNumber = 0;
                foreach (var nodeIndex in TreeState.Visible(element))
                {
                    var top = y + rowNumber++ * 24;
                    if (top + 24 > bottom) break;
                    if (nodeIndex == ElementState.Index(element)) Box(svg, x, top, element.Width, 24, "#e5e7eb");
                    var left = x + 8 + element.TreeDepths[nodeIndex] * 16;
                    var prefix = TreeState.IsBranch(element, nodeIndex) ? (element.ExpandedNodes.Contains(nodeIndex) ? "▾ " : "▸ ") : "";
                    Text(svg, left, top + 16, prefix + ElementDefaults.SplitOption(ElementState.Items(element)[nodeIndex]).Label,
                        size, nodeIndex == ElementState.Index(element) ? "700" : weight, 1);
                }
                break;
            }

            case WireframeKind.Progress:
                WriteProgress(svg, element, x, y);
                break;

            case WireframeKind.Slider:
                if (element.Vertical)
                {
                    var handleY = bottom - element.Height * ControlRange.Percent(element) / 100;
                    Line(svg, middle, y, middle, bottom, 1);
                    Box(svg, middle - 8, handleY - 5, 16, 10, Paper);
                }
                else
                {
                    var handleX = x + element.Width * ControlRange.Percent(element) / 100;
                    Line(svg, x, centerY, right, centerY, 1);
                    Box(svg, handleX - 5, centerY - 8, 10, 16, Paper);
                }
                break;

            case WireframeKind.Badge:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, middle, centerY + (size / 3), text, size, weight, 1, "middle");
                break;

            case WireframeKind.Avatar:
                Circle(svg, middle, centerY, Math.Min(element.Width, element.Height) / 2, Paper);
                Text(svg, middle, centerY + (size / 3), text, size, "700", 1, "middle");
                break;

            case WireframeKind.Calendar:
                WriteCalendar(svg, element, x, y, size);
                break;

            case WireframeKind.Map:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Line(svg, x, bottom, right - 20, y + 20, 0.6);
                Line(svg, x + 10, y, right, bottom - 24, 0.6);
                Circle(svg, middle, centerY - 6, 6, Ink);
                Tri(svg, middle - 7, centerY, middle + 7, centerY, middle, centerY + 14);
                if (!string.IsNullOrWhiteSpace(element.Text)) Text(svg, middle, bottom - 6, element.Text, 11, weight, .85, "middle");
                break;

            case WireframeKind.Video:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Tri(svg, middle - 10, centerY - 12, middle - 10, centerY + 12, middle + 12, centerY);
                if (!string.IsNullOrWhiteSpace(element.Text)) Text(svg, middle, bottom - 6, element.Text, 11, weight, .85, "middle");
                break;


            default:
                WriteExtra(svg, element, x, y, size);
                break;
        }

        if (FormState.HasVisualState(element.Kind) && element.VisualState is ControlVisualState.Focused or ControlVisualState.Error)
        {
            svg.Append("<rect x=\"").Append(Number(x + 2)).Append("\" y=\"").Append(Number(y + 2))
                .Append("\" width=\"").Append(Number(Math.Max(0, element.Width - 4))).Append("\" height=\"").Append(Number(Math.Max(0, element.Height - 4)))
                .Append("\" fill=\"none\" stroke=\"#1b1e24\" stroke-width=\"2\"")
                .Append(element.VisualState == ControlVisualState.Error ? " stroke-dasharray=\"4 2\"" : "").Append("/>");
        }
        if (FormState.HasFeedback(element.Kind) && !string.IsNullOrEmpty(element.FeedbackText))
            Text(svg, x + 6, bottom - 2, element.FeedbackText, 10, "400", 1);
        svg.AppendLine("</g>");
    }

    /// <summary>Writes a block of text as one line per newline, stopping at the bottom of the block.</summary>
    private static void WriteLines(StringBuilder svg, string text, double x, double firstBaseline, double size, double limit)
    {
        var baseline = firstBaseline;

        foreach (var line in text.Split('\n'))
        {
            if (baseline > limit)
            {
                return;
            }

            Text(svg, x, baseline, line, size, "400", 1);
            baseline += size * 1.35;
        }
    }

    private static void Box(StringBuilder svg, double x, double y, double width, double height, string fill) =>
        svg.Append("<rect x=\"").Append(Number(x))
            .Append("\" y=\"").Append(Number(y))
            .Append("\" width=\"").Append(Number(width))
            .Append("\" height=\"").Append(Number(height))
            .Append("\" fill=\"").Append(fill)
            .Append("\" stroke=\"").Append(Ink)
            .Append("\" stroke-width=\"1\"/>");

    private static void Line(StringBuilder svg, double x1, double y1, double x2, double y2, double opacity) =>
        svg.Append("<line x1=\"").Append(Number(x1))
            .Append("\" y1=\"").Append(Number(y1))
            .Append("\" x2=\"").Append(Number(x2))
            .Append("\" y2=\"").Append(Number(y2))
            .Append("\" stroke=\"").Append(Ink)
            .Append("\" stroke-width=\"1\"")
            .Append(opacity >= 1 ? string.Empty : " stroke-opacity=\"" + Number(opacity) + "\"")
            .Append("/>");

    private static void Circle(StringBuilder svg, double cx, double cy, double radius, string fill, string stroke = Ink) =>
        svg.Append("<circle cx=\"").Append(Number(cx))
            .Append("\" cy=\"").Append(Number(cy))
            .Append("\" r=\"").Append(Number(radius))
            .Append("\" fill=\"").Append(fill)
            .Append("\" stroke=\"").Append(stroke)
            .Append("\" stroke-width=\"1\"/>");

    private static void Tri(StringBuilder svg, double x1, double y1, double x2, double y2, double x3, double y3) =>
        svg.Append("<polygon points=\"")
            .Append(Number(x1)).Append(',').Append(Number(y1)).Append(' ')
            .Append(Number(x2)).Append(',').Append(Number(y2)).Append(' ')
            .Append(Number(x3)).Append(',').Append(Number(y3))
            .Append("\" fill=\"none\" stroke=\"").Append(Ink).Append("\" stroke-width=\"1\"/>");

    private static void Text(
        StringBuilder svg,
        double x,
        double y,
        string text,
        double size,
        string weight,
        double opacity,
        string anchor = "start") =>
        svg.Append("<text x=\"").Append(Number(x))
            .Append("\" y=\"").Append(Number(y))
            .Append("\" font-family=\"").Append(Font)
            .Append("\" font-size=\"").Append(Number(size))
            .Append("\" font-weight=\"").Append(weight)
            .Append("\" text-anchor=\"").Append(anchor)
            .Append("\" fill=\"").Append(Ink)
            .Append('"')
            .Append(opacity >= 1 ? string.Empty : " opacity=\"" + Number(opacity) + "\"")
            .Append('>')
            .Append(Escape(text))
            .Append("</text>");

    /// <summary>
    /// The characters XML will not take as themselves. A wireframe that says "логин &lt;email&gt;" has to
    /// keep saying it after it was exported, and a block whose text closes a tag must not take the rest
    /// of the drawing with it.
    /// </summary>
    private static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);

    /// <summary>Numbers go into the drawing the way markup expects them, whatever language the machine speaks.</summary>
    private static string Number(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void Start(StringBuilder svg, double width, double height)
    {
        svg.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>");
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" ");
        svg.Append("width=\"").Append(Number(width)).Append("\" ");
        svg.Append("height=\"").Append(Number(height)).Append("\" ");
        svg.Append("viewBox=\"0 0 ").Append(Number(width)).Append(' ').Append(Number(height)).Append("\">");
        svg.AppendLine();
        svg.Append("<rect x=\"0\" y=\"0\" width=\"").Append(Number(width)).Append("\" height=\"")
            .Append(Number(height)).Append("\" fill=\"").Append(Paper).Append("\" stroke=\"").Append(Ink)
            .Append("\" stroke-width=\"1\"/>");
        svg.AppendLine();
    }

    private static void End(StringBuilder svg)
    {
        svg.AppendLine("</svg>");
    }
}
