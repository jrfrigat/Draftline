using System.Globalization;

namespace Draftline.App.Model;

/// <summary>Month, ISO date and real day grid shared by calendar UI and SVG.</summary>
public static class CalendarState
{
    private static readonly string[] Months = { "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь", "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь" };
    /// <summary>Monday-first day names.</summary>
    public static IReadOnlyList<string> Weekdays { get; } = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
    /// <summary>Effective first day of the displayed month.</summary>
    public static DateOnly Month(WireframeElement e) => DateOnly.TryParseExact(e.CalendarMonth + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var month)
        ? month : new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
    /// <summary>Number of blank cells before the first Monday-based day.</summary>
    public static int Offset(WireframeElement e) => ((int)Month(e).DayOfWeek + 6) % 7;
    /// <summary>Number of rows needed by the month's days.</summary>
    public static int Weeks(WireframeElement e) => (Offset(e) + Days(e) + 6) / 7;
    /// <summary>Real month length, including leap years.</summary>
    public static int Days(WireframeElement e) => DateTime.DaysInMonth(Month(e).Year, Month(e).Month);
    /// <summary>Visible caption, optionally prefixed by custom text.</summary>
    public static string Title(WireframeElement e) => (string.IsNullOrWhiteSpace(e.Text) || e.Text == "Месяц" ? "" : e.Text + " · ") + Months[Month(e).Month - 1] + " " + Month(e).Year;
    /// <summary>ISO date of a numbered day.</summary>
    public static string Date(WireframeElement e, int day) => new DateOnly(Month(e).Year, Month(e).Month, day).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    /// <summary>Whether shifting stays within supported years.</summary>
    public static bool CanShift(WireframeElement e, int direction) => direction < 0 ? Month(e) > DateOnly.MinValue : Month(e).Year < 9999 || Month(e).Month < 12;
    /// <summary>Change month and discard an out-of-month selection.</summary>
    public static void Shift(WireframeElement e, int direction)
    {
        if (!CanShift(e, direction)) return;
        e.CalendarMonth = Month(e).AddMonths(direction).ToString("yyyy-MM", CultureInfo.InvariantCulture); Normalize(e);
    }
    /// <summary>Select an ISO date and reveal its month.</summary>
    public static void SetDate(WireframeElement e, string? text)
    {
        e.SelectedDate = DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";
        if (e.SelectedDate.Length > 0) e.CalendarMonth = e.SelectedDate[..7];
        Normalize(e);
    }
    /// <summary>Safe persisted month and date for new and old documents.</summary>
    public static void Normalize(WireframeElement e)
    {
        if (e.Kind != WireframeKind.Calendar) return;
        e.CalendarMonth = Month(e).ToString("yyyy-MM", CultureInfo.InvariantCulture);
        if (!DateOnly.TryParseExact(e.SelectedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) || day.Year != Month(e).Year || day.Month != Month(e).Month)
            e.SelectedDate = "";
    }
}
