using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;

public static partial class SvgExporter
{
    private static void WriteChoices(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        var items = e.ChoiceItems;
        if (e.Kind == WireframeKind.Select)
        {
            Box(svg, x, y, e.Width, e.Height, Paper);
            Text(svg, x + 6, y + e.Height / 2 + 4, ElementState.DisplayChoice(e), size, "400", 1);
            Text(svg, x + e.Width - 16, y + e.Height / 2 + 4, ElementState.IsExpanded(e) ? "▴" : "▾", size, "400", 1);
            if (ElementState.IsExpanded(e)) DrawChoiceRows(svg, e, NavItems.Rows(items).ToList(), x, y + e.Height, e.Width, size);
        }
        else if (e.Kind == WireframeKind.List)
        {
            Box(svg, x, y, e.Width, e.Height, Paper);
            DrawChoiceRows(svg, e, NavItems.Rows(items).ToList(), x, y, e.Width, size, e.Height);
        }
        else
        {
            var width = e.Width / Math.Max(1, items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                ChoiceRow(svg, e, new(i.ToString(), items[i], 0), x + i * width, y, width, e.Height, size);
                if (e.Kind == WireframeKind.Breadcrumbs && i < items.Count - 1) Text(svg, x + (i + 1) * width - 8, y + e.Height / 2 + 4, "›", size, "400", .6);
            }
            if (e.Kind == WireframeKind.MenuBar && ElementState.IsExpanded(e))
            {
                var bounds = NavItems.MenuBounds(e);
                DrawChoiceRows(svg, e, NavItems.MenuRows(e), x + bounds.X - e.X, y + e.Height, bounds.Width, size);
            }
        }
    }
    private static void DrawChoiceRows(StringBuilder svg, WireframeElement e, IReadOnlyList<NavRow> rows, double x, double y, double width, double size, double limit = double.MaxValue)
    {
        var count = Math.Min(NavItems.VisibleCount(e, rows.Count), (int)Math.Min(int.MaxValue, Math.Floor(limit / 24)));
        if (count == 0) return;
        Box(svg, x, y, width, count * 24, Paper);
        for (var i = 0; i < count; i++) ChoiceRow(svg, e, rows[i], x, y + i * 24, width, 24, size);
        if (count < rows.Count) Line(svg, x + width - 3, y + 3, x + width - 3, y + count * 24 - 3, .5);
    }
    private static void ChoiceRow(StringBuilder svg, WireframeElement e, NavRow row, double x, double y, double width, double height, double size)
    {
        if (row.Item.Separator) { Line(svg, x + 6, y + height / 2, x + width - 6, y + height / 2, .6); return; }
        var selected = e.SelectedPath.Length > 0 ? e.SelectedPath == row.Path : row.Path == ElementState.Index(e).ToString();
        if (selected) Box(svg, x, y, width, height, "#e5e7eb");
        var opacity = row.Item.Disabled ? .45 : 1;
        Text(svg, x + 8 + row.Depth * 16, y + height / 2 + 4, row.Item.Label + (row.Item.Children.Count > 0 ? row.Item.Expanded ? " ▾" : " ▸" : ""), size, "400", opacity);
        if (row.Item.Value.Length > 0) Text(svg, x + width - 8, y + height / 2 + 4, row.Item.Value, size, "400", opacity * .7, "end");
    }
}
