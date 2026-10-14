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
public static class SvgExporter
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
        foreach (var element in layer.InDrawOrder().OrderBy(element =>
                     element.Kind == WireframeKind.Select && ElementState.IsExpanded(element) ? 1 : 0))
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
    public static string ExportElement(WireframeElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var width = Math.Max(1, element.Width);
        var height = Math.Max(1, ElementState.DrawingHeight(element));
        var svg = new StringBuilder();

        Start(svg, width, height);
        WriteElement(svg, element, offsetX: -element.X, offsetY: -element.Y);
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
                Box(svg, x, y, element.Width, element.Height, Paper);
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
                if (element.Checked)
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
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, x + 6, centerY + (size / 3), text, size, weight, 1);
                Text(svg, right - 22, centerY + (size / 3), ElementState.DisplayChoice(element), size, "400",
                    element.ShowsHint && ElementState.Index(element) < 0 ? 0.55 : 1, "end");
                Tri(svg, right - 20, centerY - 3, right - 8, centerY - 3, right - 14, centerY + 4);
                if (ElementState.IsExpanded(element))
                {
                    var choices = ElementState.Items(element);
                    Box(svg, x, bottom, element.Width, choices.Count * ElementState.DropdownRowHeight, Paper);
                    for (var choiceIndex = 0; choiceIndex < choices.Count; choiceIndex++)
                    {
                        var choiceTop = bottom + choiceIndex * ElementState.DropdownRowHeight;
                        if (choiceIndex == ElementState.Index(element))
                            Box(svg, x, choiceTop, element.Width, ElementState.DropdownRowHeight, "#e5e7eb");
                        var choice = ElementDefaults.SplitOption(choices[choiceIndex]);
                        Text(svg, x + 8, choiceTop + 16, choice.Label, size, "400", 1);
                        if (!string.IsNullOrEmpty(choice.Value))
                            Text(svg, right - 8, choiceTop + 16, choice.Value, size, "400", 0.7, "end");
                    }
                }
                break;

            case WireframeKind.List:
                Box(svg, x, y, element.Width, element.Height, Paper);

                var rows = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var rowHeight = element.Height / Math.Max(1, rows.Count);
                var index = 0;

                foreach (var row in rows)
                {
                    var rowTop = y + (rowHeight * index);
                    var (label, value) = ElementDefaults.SplitOption(row);

                    if (index == ElementState.Index(element))
                        Box(svg, x, rowTop, element.Width, rowHeight, "#e5e7eb");

                    Text(svg, x + 8, rowTop + (rowHeight / 2) + 4, label, size, weight, 1);

                    // A row may carry a value, and in a list the value belongs on the right: the eye
                    // reads a column of numbers down the edge, not scattered through the sentences.
                    if (!string.IsNullOrEmpty(value))
                    {
                        Text(svg, right - 8, rowTop + (rowHeight / 2) + 4, value, size, "400", 0.7, "end");
                    }

                    index++;
                    if (index < rows.Count)
                    {
                        Line(svg, x, rowTop + rowHeight, right, rowTop + rowHeight, 1);
                    }
                }

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
                Line(svg, x, centerY, right, centerY, 1);
                break;

            case WireframeKind.DataGrid:
            {
                Box(svg, x, y, element.Width, element.Height, Paper);

                var columns = element.Columns.Count;
                var columnWidth = element.Width / Math.Max(1, columns);
                var headerHeight = 24d;
                var dataRowHeight = element.Rows.Count > 0
                    ? Math.Max(14, (element.Height - headerHeight) / element.Rows.Count)
                    : 0d;

                for (var column = 0; column < columns; column++)
                {
                    Text(svg, x + (columnWidth * column) + 6, y + 16, element.Columns[column], size, "700", 1);

                    if (column > 0)
                    {
                        Line(svg, x + (columnWidth * column), y, x + (columnWidth * column), bottom, 0.5);
                    }
                }

                Line(svg, x, y + headerHeight, right, y + headerHeight, 1);

                for (var rowIndex = 0; rowIndex < element.Rows.Count; rowIndex++)
                {
                    var rowTop = y + headerHeight + (dataRowHeight * rowIndex);

                    for (var column = 0; column < element.Rows[rowIndex].Count; column++)
                    {
                        Text(svg, x + (columnWidth * column) + 6, rowTop + (dataRowHeight / 2) + 4,
                            element.Rows[rowIndex][column], size, "400", 1);
                    }

                    if (rowIndex > 0)
                    {
                        Line(svg, x, rowTop, right, rowTop, 0.5);
                    }
                }

                break;
            }

            case WireframeKind.Tabs:
            {
                var tabs = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var tabWidth = element.Width / Math.Max(1, tabs.Count);

                for (var tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
                {
                    var tabLeft = x + (tabWidth * tabIndex);
                    var (label, _) = ElementDefaults.SplitOption(tabs[tabIndex]);

                    Text(svg, tabLeft + (tabWidth / 2), centerY + (size / 3), label, size,
                        tabIndex == ElementState.Index(element) ? "700" : "400", tabIndex == ElementState.Index(element) ? 1 : 0.7, "middle");
                    if (tabIndex == ElementState.Index(element))
                        Line(svg, tabLeft, bottom - 2, tabLeft + tabWidth, bottom - 2, 2);
                    Line(svg, tabLeft, y, tabLeft, bottom, 1);
                }

                Line(svg, x, bottom, right, bottom, 1);
                break;
            }

            case WireframeKind.MenuBar:
            {
                var menuItems = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var menuLeft = x + 8;
                var menuIndex = 0;

                foreach (var item in menuItems)
                {
                    var (label, _) = ElementDefaults.SplitOption(item);
                    Text(svg, menuLeft, centerY + (size / 3), label, size,
                        menuIndex == ElementState.Index(element) ? "700" : "400", 1);
                    menuLeft += (label.Length * size * 0.62) + 18;
                    menuIndex++;
                }

                break;
            }

            case WireframeKind.Accordion:
            {
                var sections = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var sectionHeight = element.Height / Math.Max(1, sections.Count + (ElementState.IsExpanded(element) ? 1 : 0));
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
                        Text(svg, x + 8, sectionTop + (sectionHeight / 2) + 4,
                            string.IsNullOrWhiteSpace(element.SecondaryText) ? "Содержимое раздела" : element.SecondaryText,
                            size, "400", 1);
                        sectionTop += sectionHeight;
                    }
                }

                Box(svg, x, y, element.Width, element.Height, "none");
                break;
            }

            case WireframeKind.Tree:
            {
                var nodes = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;

                for (var nodeIndex = 0; nodeIndex < nodes.Count; nodeIndex++)
                {
                    if (nodeIndex > 0 && !ElementState.IsExpanded(element)) break;
                    var (label, _) = ElementDefaults.SplitOption(nodes[nodeIndex]);
                    var itemTop = y + (size * 1.5 * nodeIndex) + size;

                    // The first row is the parent and the rest hang under it: a tree in a wireframe is
                    // about showing that something nests, not about drawing branches.
                    if (nodeIndex == 0)
                    {
                        Text(svg, x + 4, itemTop, ElementState.IsExpanded(element) ? "▾" : "▸", size, "400", 0.8);
                    }
                    if (nodeIndex == ElementState.Index(element))
                        Box(svg, x, itemTop - size, element.Width, size * 1.5, "#e5e7eb");
                    Text(svg, x + (nodeIndex == 0 ? 18 : 24), itemTop, label, size,
                        nodeIndex == ElementState.Index(element) || nodeIndex == 0 ? "700" : "400", 1);
                }

                break;
            }

            case WireframeKind.Progress:
            {
                Box(svg, x, y, element.Width, element.Height, Paper);

                svg.Append("<rect x=\"").Append(Number(x)).Append("\" y=\"").Append(Number(y))
                    .Append("\" width=\"").Append(Number(element.Width * PercentOf(element) / 100))
                    .Append("\" height=\"").Append(Number(element.Height))
                    .Append("\" fill=\"").Append(Ink).Append("\"/>");
                break;
            }

            case WireframeKind.Slider:
            {
                var handleX = x + (element.Width * PercentOf(element) / 100);

                Line(svg, x, centerY, right, centerY, 1);
                Box(svg, handleX - 5, centerY - 8, 10, 16, Paper);
                break;
            }

            case WireframeKind.Badge:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, middle, centerY + (size / 3), text, size, weight, 1, "middle");
                break;

            case WireframeKind.Avatar:
                Circle(svg, middle, centerY, Math.Min(element.Width, element.Height) / 2, Paper);
                Text(svg, middle, centerY + (size / 3), text, size, "700", 1, "middle");
                break;

            case WireframeKind.Calendar:
            {
                var cellWidth = element.Width / 7;
                var cellHeight = (element.Height - 24) / 4;

                Box(svg, x, y, element.Width, element.Height, "none");
                Text(svg, x + 6, y + 16, text, size, "700", 1);
                Line(svg, x, y + 24, right, y + 24, 0.5);

                for (var day = 1; day <= 28; day++)
                {
                    var cell = day - 1;
                    var dayColumn = cell % 7;
                    var dayRow = cell / 7;

                    Text(
                        svg,
                        x + (cellWidth * dayColumn) + (cellWidth / 2),
                        y + 34 + (cellHeight * dayRow) + (cellHeight / 2),
                        day.ToString(CultureInfo.InvariantCulture),
                        10,
                        "400",
                        0.8,
                        "middle");

                    Line(svg, x + (cellWidth * dayColumn), y + 24, x + (cellWidth * dayColumn), bottom, 0.3);
                }

                break;
            }

            case WireframeKind.Map:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Line(svg, x, bottom, right - 20, y + 20, 0.6);
                Line(svg, x + 10, y, right, bottom - 24, 0.6);
                Circle(svg, middle, centerY - 6, 6, Ink);
                Tri(svg, middle - 7, centerY, middle + 7, centerY, middle, centerY + 14);
                break;

            case WireframeKind.Video:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Tri(svg, middle - 10, centerY - 12, middle - 10, centerY + 12, middle + 12, centerY);
                break;

            case WireframeKind.Breadcrumbs:
            {
                var crumbs = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var crumbLeft = x + 4;

                foreach (var crumb in crumbs)
                {
                    var (label, _) = ElementDefaults.SplitOption(crumb);

                    Text(svg, crumbLeft, centerY + (size / 3), label, size, "400", 0.85);
                    crumbLeft += (label.Length * size * 0.62) + 20;
                    Text(svg, crumbLeft - 14, centerY + (size / 3), "›", size, "400", 0.5);
                }

                break;
            }

            case WireframeKind.StatusBar:
            {
                var cells = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var cellWidth = element.Width / Math.Max(1, cells.Count);

                Box(svg, x, y, element.Width, element.Height, Paper);

                for (var cellIndex = 0; cellIndex < cells.Count; cellIndex++)
                {
                    var (label, _) = ElementDefaults.SplitOption(cells[cellIndex]);

                    Text(svg, x + (cellWidth * cellIndex) + 8, centerY + (size / 3), label, 10, "400", 0.85);

                    if (cellIndex > 0)
                    {
                        Line(svg, x + (cellWidth * cellIndex), y + 3, x + (cellWidth * cellIndex), bottom - 3, 0.4);
                    }
                }

                break;
            }
        }

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

    /// <summary>
    /// The share of its range a bar or a slider shows, as a percentage between 0 and 100. An empty or
    /// unreadable value means the middle of the range, which is where the block lands.
    /// </summary>
    private static double PercentOf(WireframeElement element) =>
        double.TryParse(
            element.Value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var parsed)
            ? Math.Clamp(parsed, 0, 100)
            : 60;

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
