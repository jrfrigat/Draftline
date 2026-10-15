using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;

public static partial class SvgExporter
{
    private static void WriteProgress(StringBuilder svg, WireframeElement e, double x, double y)
    {
        Box(svg, x, y, e.Width, e.Height, Paper);
        if (!e.Indeterminate)
        {
            svg.Append($"<rect x=\"{Number(x)}\" y=\"{Number(y)}\" width=\"{Number(e.Width * ControlRange.Percent(e) / 100)}\" height=\"{Number(e.Height)}\" fill=\"{Ink}\"/>");
            return;
        }
        var left = x + e.Width * .3; var right = left + e.Width * .4;
        for (var start = left - e.Height; start < right; start += 6)
            Line(svg, Math.Max(left, start), y + Math.Max(0, left - start), Math.Min(right, start + e.Height), y + Math.Min(e.Height, right - start), 1);
    }
    private static void WriteCalendar(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        var cw = e.Width / 7; var rh = Math.Max(0, e.Height - 48) / CalendarState.Weeks(e);
        Box(svg, x, y, e.Width, e.Height, Paper);
        Text(svg, x + 6, y + 18, CalendarState.Title(e), size, "700", 1);
        Line(svg, x, y + 28, x + e.Width, y + 28, 1);
        for (var i = 0; i < 7; i++) Text(svg, x + (i + .5) * cw, y + 42, CalendarState.Weekdays[i], 10, "700", .7, "middle");
        for (var day = 1; day <= CalendarState.Days(e); day++)
        {
            var cell = CalendarState.Offset(e) + day - 1; var left = x + cell % 7 * cw; var top = y + 48 + cell / 7 * rh;
            if (e.SelectedDate == CalendarState.Date(e, day)) Box(svg, left, top, cw, rh, "#e5e7eb");
            Text(svg, left + cw / 2, top + rh / 2 + 4, day.ToString(System.Globalization.CultureInfo.InvariantCulture), 10, "400", 1, "middle");
        }
    }
}
