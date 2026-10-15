using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;

public static partial class SvgExporter
{
    private static void WriteTable(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        var cols = e.Columns.Count;
        var cw = e.Width / Math.Max(1, cols);
        var hh = Math.Min(28, e.Height);
        var rows = TableState.Ordered(e);
        var rh = (e.Height - hh) / Math.Max(1, rows.Count);
        Box(svg, x, y, e.Width, e.Height, Paper);
        for (var i = 0; i < rows.Count; i++)
        {
            var top = y + hh + i * rh;
            var fill = rows[i].Index == e.SelectedRow ? "#e5e7eb" : e.TableStyle == TableStyle.Zebra && i % 2 == 1 ? "#f3f4f6" : "";
            if (fill.Length > 0) svg.Append($"<rect x=\"{Number(x)}\" y=\"{Number(top)}\" width=\"{Number(e.Width)}\" height=\"{Number(rh)}\" fill=\"{fill}\"/>");
            for (var c = 0; c < rows[i].Cells.Count; c++) Text(svg, x + c * cw + 6, top + rh / 2 + 4, rows[i].Cells[c], size, "400", 1);
            if (i > 0 && e.TableStyle is TableStyle.Grid or TableStyle.Rows) Line(svg, x, top, x + e.Width, top, .5);
        }
        for (var c = 0; c < cols; c++)
        {
            Text(svg, x + c * cw + 6, y + hh / 2 + 4, TableState.Header(e, c), size, "700", 1);
            if (c > 0 && e.TableStyle is TableStyle.Grid or TableStyle.Columns) Line(svg, x + c * cw, y, x + c * cw, y + e.Height, .5);
        }
        Line(svg, x, y + hh, x + e.Width, y + hh, 1);
    }
}
