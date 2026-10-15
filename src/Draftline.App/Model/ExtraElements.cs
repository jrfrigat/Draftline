using System.Globalization;

namespace Draftline.App.Model;

/// <summary>Definitions for additional low-fidelity palette kinds.</summary>
public static class ExtraElements
{
    /// <summary>A palette item's caption and initial geometry.</summary>
    public sealed record Definition(string Name, double Width, double Height, string Text);
    /// <summary>New controls, keyed by the persisted kind.</summary>
    public static IReadOnlyDictionary<WireframeKind, Definition> Definitions { get; } = new Dictionary<WireframeKind, Definition>
    {
        [WireframeKind.Link] = new("Ссылка",160,24,"Ссылка"),
        [WireframeKind.Icon] = new("Иконка",40,40,"★"),
        [WireframeKind.Switch] = new("Тумблер",180,32,"Включено"),
        [WireframeKind.Stepper] = new("Счётчик",140,32,""),
        [WireframeKind.Tooltip] = new("Подсказка",200,80,"Подсказка"),
        [WireframeKind.Popover] = new("Всплывающая панель",240,120,"Подробнее"),
        [WireframeKind.DatePicker] = new("Выбор даты",180,36,"Дата"),
        [WireframeKind.TimePicker] = new("Выбор времени",140,36,"Время"),
        [WireframeKind.ColorPicker] = new("Выбор цвета",160,36,"Цвет")
    };
    /// <summary>Additional palette sections.</summary>
    public static IReadOnlyList<(string Title, IReadOnlyList<WireframeKind> Kinds)> Sections => new[]
    {
        ("Дополнительные формы", (IReadOnlyList<WireframeKind>)Definitions.Keys.ToArray())
    };
    /// <summary>Whether a kind uses the shared SVG-fragment view.</summary>
    public static bool Is(WireframeKind kind) => Definitions.ContainsKey(kind);
    /// <summary>Whether the kind accepts a value.</summary>
    public static bool HasValue(WireframeKind kind) => kind is WireframeKind.Link or WireframeKind.Stepper or WireframeKind.DatePicker or WireframeKind.TimePicker or WireframeKind.ColorPicker;
    /// <summary>Safe monochrome/default colour or a valid authored hexadecimal colour.</summary>
    public static string Color(string? text) => text is { Length:7 } && text[0]=='#' && text[1..].All(Uri.IsHexDigit) ? text : "#999999";
    /// <summary>Additional defaults without changing the meaning of existing kinds.</summary>
    public static void Initialize(WireframeElement e)
    {
        if (e.Kind == WireframeKind.Stepper) e.Value = "0";
        if (e.Kind == WireframeKind.ColorPicker) e.Value = "#999999";
        if (e.Kind is WireframeKind.Tooltip or WireframeKind.Popover) { e.Expanded = true; e.SecondaryText = "Пояснение"; }
    }
    /// <summary>Normalize imported picker values.</summary>
    public static void Normalize(WireframeElement e)
    {
        if (e.Kind == WireframeKind.ColorPicker) e.Value = Color(e.Value);
        if (e.Kind == WireframeKind.DatePicker && e.Value.Length > 0 && !DateOnly.TryParseExact(e.Value,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _)) e.Value="";
        if (e.Kind == WireframeKind.TimePicker && e.Value.Length > 0 && !TimeOnly.TryParseExact(e.Value,"HH:mm",CultureInfo.InvariantCulture,DateTimeStyles.None,out _)) e.Value="";
    }
}
