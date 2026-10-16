using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;
public static partial class SvgExporter
{
    private static void WriteFrame(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        var w = e.Width;
        var h = e.Height;
        var cx = x + w / 2;
        var cy = y + h / 2;
        var items = ElementState.Items(e);
        switch (e.Kind)
        {
            case WireframeKind.Device:
                Box(svg, x, y, w, h, Paper);
                Box(svg, x + 8, y + 22, Math.Max(1, w - 16), Math.Max(1, h - 48), Paper);
                Line(svg, cx - 12, y + 10, cx + 12, y + 10, 1);
                Circle(svg, cx, y + h - 12, 5, Paper);
                break;
            case WireframeKind.Browser:
                Box(svg, x, y, w, h, Paper);
                Line(svg, x, y + 28, x + w, y + 28, 1);
                Text(svg, x + 6, y + 18, "‹  ›   " + (e.Value.Length > 0 ? e.Value : e.DisplayText), 10, "400", 1);
                Text(svg, x + 8, y + 48, e.DisplayText, size, "400", .7);
                break;
            case WireframeKind.Fieldset:
                Box(svg, x, y + 8, w, Math.Max(1, h - 8), Paper);
                Box(svg, x + 8, y, Math.Min(w - 16, Math.Max(40, e.DisplayText.Length * size * .65)), 18, Paper);
                Text(svg, x + 12, y + 13, e.DisplayText, size, "400", 1);
                break;
            case WireframeKind.Scrollbar:
                Box(svg, x, y, w, h, Paper);
                var share = Math.Clamp(ControlRange.Percent(e) / 100, 0, 1);
                if (e.Vertical)
                {
                    var thumb = Math.Max(8, h * .2);
                    Box(svg, x + 2, y + share * Math.Max(0, h - thumb), Math.Max(1, w - 4), thumb, "#e5e7eb");
                }
                else
                {
                    var thumb = Math.Max(8, w * .2);
                    Box(svg, x + share * Math.Max(0, w - thumb), y + 2, thumb, Math.Max(1, h - 4), "#e5e7eb");
                }

                break;
            case WireframeKind.Splitter:
                if (e.Vertical)
                {
                    Line(svg, cx - 2, y, cx - 2, y + h, .5);
                    Line(svg, cx + 2, y, cx + 2, y + h, .5);
                    Text(svg, cx, cy + 4, "⋮", size, "400", 1, "middle");
                }
                else
                {
                    Line(svg, x, cy - 2, x + w, cy - 2, .5);
                    Line(svg, x, cy + 2, x + w, cy + 2, .5);
                    Text(svg, cx, cy + 4, "⋯", size, "400", 1, "middle");
                }

                break;
            case WireframeKind.AudioPlayer:
                Box(svg, x, y, w, h, Paper);
                Text(svg, x + 14, cy + 4, e.Checked ? "Ⅱ" : "▶", size, "400", 1);
                Text(svg, x + 40, cy + 4, e.DisplayText + "  " + e.Value, size, "400", 1);
                break;
            case WireframeKind.RichTextEditor:
                Box(svg, x, y, w, h, Paper);
                Line(svg, x, y + 28, x + w, y + 28, 1);
                Text(svg, x + 8, y + 18, "B   I   U   ≡   ☷", size, "700", 1);
                WriteLines(svg, e.Value, x + 8, y + 48, size, y + h - 4);
                break;
            case WireframeKind.AppBar:
                Box(svg, x, y, w, h, Paper);
                Text(svg, x + 8, cy + 4, "☰  " + e.DisplayText, size, "700", 1);
                Text(svg, x + w - 8, cy + 4, string.Join("  ", items.Select(item => ElementDefaults.SplitOption(item).Label)), 10, "400", 1, "end");
                break;
            case WireframeKind.Alert:
                Box(svg, x, y, w, h, Paper);
                Text(svg, x + 8, y + 20, e.DisplayText, size, "700", 1);
                Line(svg, x, y + 28, x + w, y + 28, 1);
                WriteLines(svg, e.SecondaryText, x + 8, y + 48, size, y + h - 42);
                var aw = w / Math.Max(1, items.Count);
                for (var i = 0; i < items.Count; i++)
                {
                    Box(svg, x + i * aw + 4, y + h - 36, Math.Max(1, aw - 8), 28, i == ElementState.Index(e) ? "#e5e7eb" : Paper);
                    Text(svg, x + (i + .5) * aw, y + h - 18, ElementDefaults.SplitOption(items[i]).Label, size, "400", 1, "middle");
                }

                break;
            case WireframeKind.CoverFlow:
                var step = w / Math.Max(1, items.Count);
                for (var i = 0; i < items.Count; i++)
                {
                    var chosen = i == ElementState.Index(e);
                    var left = x + i * step + 4;
                    var top = y + (chosen ? 8 : 24);
                    var height = Math.Max(1, h - (chosen ? 36 : 60));
                    Box(svg, left, top, Math.Max(1, step - 8), height, chosen ? "#e5e7eb" : Paper);
                    Line(svg, left, top, left + step - 8, top + height, .5);
                    Line(svg, left + step - 8, top, left, top + height, .5);
                    Text(svg, left + step / 2, top + height + 18, ElementDefaults.SplitOption(items[i]).Label, 10, "400", 1, "middle");
                }

                break;
            case WireframeKind.IosKeyboard:
                Box(svg, x, y, w, h, Paper);
                var rowHeight = h / Math.Max(1, items.Count);
                for (var row = 0; row < items.Count; row++)
                {
                    var keys = items[row].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var keyWidth = w / Math.Max(1, keys.Length);
                    for (var c = 0; c < keys.Length; c++)
                    {
                        Box(svg, x + c * keyWidth + 2, y + row * rowHeight + 2, Math.Max(1, keyWidth - 4), Math.Max(1, rowHeight - 4), Paper);
                        Text(svg, x + (c + .5) * keyWidth, y + (row + .5) * rowHeight + 4, keys[c], 10, "400", 1, "middle");
                    }
                }

                break;
            case WireframeKind.Camera:
                Box(svg, x, y, w, h, Paper);
                Circle(svg, cx, cy, Math.Min(w, h) * .18, Paper);
                Circle(svg, cx, cy, Math.Min(w, h) * .13, Paper);
                Text(svg, cx, y + h - 8, e.DisplayText, size, "400", 1, "middle");
                Line(svg, x + 10, y + 10, x + 30, y + 10, 1);
                Line(svg, x + 10, y + 10, x + 10, y + 30, 1);
                Line(svg, x + w - 10, y + 10, x + w - 30, y + 10, 1);
                Line(svg, x + w - 10, y + 10, x + w - 10, y + 30, 1);
                break;
        }
    }
}
