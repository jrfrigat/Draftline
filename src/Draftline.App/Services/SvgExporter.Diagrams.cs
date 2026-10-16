using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;
public static partial class SvgExporter
{
    private static void Polygon(StringBuilder svg, IEnumerable<(double X, double Y)> points, string fill = Paper) => svg.Append("<polygon points=\"").Append(string.Join(' ', points.Select(p => Number(p.X) + "," + Number(p.Y)))).Append($"\" fill=\"{fill}\" stroke=\"{Ink}\"/>");
    private static void WriteDiagram(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        var w = e.Width;
        var h = e.Height;
        var cx = x + w / 2;
        var cy = y + h / 2;
        switch (e.Kind)
        {
            case WireframeKind.Note:
                Polygon(svg, new[] { (x, y), (x + w - 14, y), (x + w, y + 14), (x + w, y + h), (x, y + h) });
                Line(svg, x + w - 14, y, x + w - 14, y + 14, 1);
                Line(svg, x + w - 14, y + 14, x + w, y + 14, 1);
                WriteLines(svg, e.DisplayText, x + 8, y + size + 8, size, y + h - 4);
                break;
            case WireframeKind.Callout:
                Polygon(svg, new[] { (x, y), (x + w, y), (x + w, y + h - 18), (x + 40, y + h - 18), (x + 20, y + h), (x + 20, y + h - 18), (x, y + h - 18) });
                WriteLines(svg, e.DisplayText, x + 8, y + size + 8, size, y + h - 22);
                break;
            case WireframeKind.Arrow:
                if (e.Vertical)
                {
                    Line(svg, cx, y + 4, cx, y + h - 4, 1);
                    Line(svg, cx - 8, y + h - 14, cx, y + h - 4, 1);
                    Line(svg, cx + 8, y + h - 14, cx, y + h - 4, 1);
                }
                else
                {
                    Line(svg, x + 4, cy, x + w - 4, cy, 1);
                    Line(svg, x + w - 14, cy - 8, x + w - 4, cy, 1);
                    Line(svg, x + w - 14, cy + 8, x + w - 4, cy, 1);
                }

                if (e.DisplayText.Length > 0)
                    Text(svg, cx, cy - 8, e.DisplayText, size, "400", 1, "middle");
                break;
            case WireframeKind.Rectangle:
                Box(svg, x, y, w, h, Paper);
                Text(svg, cx, cy + 4, e.DisplayText, size, "400", 1, "middle");
                break;
            case WireframeKind.Ellipse:
                svg.Append($"<ellipse cx=\"{Number(cx)}\" cy=\"{Number(cy)}\" rx=\"{Number(w / 2)}\" ry=\"{Number(h / 2)}\" fill=\"{Paper}\" stroke=\"{Ink}\"/>");
                Text(svg, cx, cy + 4, e.DisplayText, size, "400", 1, "middle");
                break;
            case WireframeKind.Diamond:
                Polygon(svg, new[] { (cx, y), (x + w, cy), (cx, y + h), (x, cy) });
                Text(svg, cx, cy + 4, e.DisplayText, size, "400", 1, "middle");
                break;
            case WireframeKind.SiteMap:
                var items = ElementState.Items(e).Take(12).ToArray();
                var width = w / Math.Max(1, items.Length);
                var rootWidth = Math.Min(120, w);
                Box(svg, cx - rootWidth / 2, y, rootWidth, 30, Paper);
                Text(svg, cx, y + 20, e.DisplayText, size, "400", 1, "middle");
                Line(svg, cx, y + 30, cx, y + 50, 1);
                Line(svg, x + width / 2, y + 50, x + w - width / 2, y + 50, 1);
                for (var i = 0; i < items.Length; i++)
                {
                    var left = x + i * width;
                    Line(svg, left + width / 2, y + 50, left + width / 2, y + h - 36, 1);
                    Box(svg, left + 4, y + h - 36, Math.Max(1, width - 8), 32, Paper);
                    Text(svg, left + width / 2, y + h - 15, ElementDefaults.SplitOption(items[i]).Label, 10, "400", 1, "middle");
                }

                break;
            default:
                WriteChart(svg, e, x, y, size);
                break;
        }
    }

    private static void WriteChart(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        var data = ExtraElements.Chart(e);
        var w = e.Width;
        var h = e.Height;
        Text(svg, x + 6, y + 16, e.DisplayText, size, "700", 1);
        if (e.Kind == WireframeKind.PieChart)
        {
            var radius = Math.Max(1, Math.Min(w * .28, (h - 30) / 2));
            var cx = x + radius + 10;
            var cy = y + 26 + radius;
            var sum = data.Sum(d => d.Value);
            var angle = -Math.PI / 2;
            Circle(svg, cx, cy, radius, Paper);
            for (var i = 0; i < data.Count; i++)
            {
                var fraction = sum > 0 ? data[i].Value / sum : 0;
                var end = angle + fraction * 2 * Math.PI;
                if (fraction > 0 && fraction < .999999)
                    svg.Append($"<path d=\"M {Number(cx)} {Number(cy)} L {Number(cx + radius * Math.Cos(angle))} {Number(cy + radius * Math.Sin(angle))} A {Number(radius)} {Number(radius)} 0 {(fraction > .5 ? 1 : 0)} 1 {Number(cx + radius * Math.Cos(end))} {Number(cy + radius * Math.Sin(end))} Z\" fill=\"{(i % 2 == 0 ? "#e5e7eb" : Paper)}\" stroke=\"{Ink}\"/>");
                else if (fraction >= .999999)
                    Circle(svg, cx, cy, radius, "#e5e7eb");
                if (y + 34 + i * 16 < y + h)
                    Text(svg, x + 2 * radius + 20, y + 34 + i * 16, data[i].Label + " " + data[i].Value.ToString(System.Globalization.CultureInfo.InvariantCulture), 10, "400", 1);
                angle = end;
            }

            return;
        }

        var left = x + 24;
        var bottom = y + h - 26;
        var plotW = Math.Max(1, w - 32);
        var plotH = Math.Max(1, h - 54);
        var max = Math.Max(1, data.Select(d => d.Value).DefaultIfEmpty(1).Max());
        Line(svg, left, y + 26, left, bottom, 1);
        Line(svg, left, bottom, x + w - 4, bottom, 1);
        var step = plotW / Math.Max(1, data.Count);
        double? previousX = null, previousY = null;
        for (var i = 0; i < data.Count; i++)
        {
            var px = left + (i + .5) * step;
            var py = bottom - data[i].Value / max * plotH;
            if (e.Kind == WireframeKind.BarChart)
                Box(svg, px - step * .3, py, step * .6, bottom - py, "#e5e7eb");
            else
            {
                if (previousX is { } ax)
                    Line(svg, ax, previousY!.Value, px, py, 1);
                Circle(svg, px, py, 3, Paper);
                previousX = px;
                previousY = py;
            }

            Text(svg, px, bottom + 16, data[i].Label, 9, "400", 1, "middle");
        }
    }
}
