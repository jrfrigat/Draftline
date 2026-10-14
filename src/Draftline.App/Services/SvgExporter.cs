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

    /// <summary>The whole sheet as an SVG drawing.</summary>
    public static string Export(DraftDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var width = Math.Max(1, document.CanvasWidth);
        var height = Math.Max(1, document.CanvasHeight);
        var svg = new StringBuilder();

        Start(svg, width, height);
        foreach (var element in document.InDrawOrder())
        {
            WriteElement(svg, element);
        }

        End(svg);
        return svg.ToString();
    }

    /// <summary>One block on a sheet of its own - what a person wants when they export a single control.</summary>
    public static string Export(DraftDocument document, WireframeElement element)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(element);

        var width = Math.Max(1, element.Width);
        var height = Math.Max(1, element.Height);
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
        svg.Append("<g>");

        switch (element.Kind)
        {
            case WireframeKind.Panel:
                Box(svg, x, y, element.Width, element.Height, "none");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Text(svg, x + 6, y + 14, text.ToUpperInvariant(), 11, "400", 0.7);
                }

                break;

            case WireframeKind.Heading:
                Text(svg, x, centerY + (size / 3), text, Math.Max(size, 20), "700", 1);
                break;

            case WireframeKind.Text:
                WriteLines(svg, text, x + 2, y + size, size, element.Height);
                break;

            case WireframeKind.Button:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, middle, centerY + (size / 3), text, size, weight, 1, "middle");
                break;

            case WireframeKind.TextField:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, x + 6, centerY + (size / 3), text, size, weight, 1);
                Line(svg, right - 6, centerY - (element.Height * 0.3), right - 6, centerY + (element.Height * 0.3), 1);
                break;

            case WireframeKind.TextArea:
                Box(svg, x, y, element.Width, element.Height, Paper);
                Text(svg, x + 6, y + size + 2, text, size, weight, 1);

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
                Tri(svg, right - 20, centerY - 3, right - 8, centerY - 3, right - 14, centerY + 4);
                break;

            case WireframeKind.List:
                Box(svg, x, y, element.Width, element.Height, Paper);

                var rows = element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;
                var rowHeight = element.Height / Math.Max(1, rows.Count);
                var index = 0;

                foreach (var row in rows)
                {
                    var rowTop = y + (rowHeight * index);
                    Text(svg, x + 8, rowTop + (rowHeight / 2) + 4, row, size, weight, 1);

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
