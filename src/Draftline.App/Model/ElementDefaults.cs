namespace Draftline.App.Model;

/// <summary>
/// What a new block looks like before anyone has touched it: the size it lands with, the words it
/// carries and the rows it comes with.
///
/// One table, because three places have to agree on it: the palette that creates the block, the
/// duplicator that copies it, and the reader of a file whose block arrived with a field missing.
/// </summary>
public static class ElementDefaults
{
    /// <summary>Size a block falls back to when its kind has no opinion.</summary>
    public const double DefaultWidth = 160;

    /// <summary>Size a block falls back to when its kind has no opinion.</summary>
    public const double DefaultHeight = 40;

    /// <summary>Every kind the palette offers, in the order it offers them.</summary>
    public static IReadOnlyList<WireframeKind> All { get; } = Enum.GetValues<WireframeKind>();

    /// <summary>Rows a list or a dropdown lands with.</summary>
    public static IReadOnlyList<string> DefaultOptions { get; } = new[] { "Первый пункт", "Второй пункт", "Третий пункт" };

    /// <summary>Columns a new table lands with.</summary>
    public static IReadOnlyList<string> DefaultColumns { get; } = new[] { "Колонка", "Колонка", "Колонка" };

    /// <summary>Rows a new table lands with, one cell per column.</summary>
    public static IReadOnlyList<IReadOnlyList<string>> DefaultRows { get; } = new[]
    {
        (IReadOnlyList<string>)new[] { "Значение", "Значение", "Значение" },
        new[] { "Значение", "Значение", "Значение" },
        new[] { "Значение", "Значение", "Значение" }
    };

    /// <summary>
    /// The palette's sections, in the order they are shown. Twenty-six kinds in one column do not fit
    /// on a screen, and a shelf nobody can see the end of is a shelf nobody uses.
    /// </summary>
    public static IReadOnlyList<(string Title, IReadOnlyList<WireframeKind> Kinds)> Sections { get; } = new[]
    {
        ("Основные", (IReadOnlyList<WireframeKind>)new[]
        {
            WireframeKind.Panel, WireframeKind.Text, WireframeKind.Heading, WireframeKind.Image,
            WireframeKind.Divider, WireframeKind.Badge, WireframeKind.Avatar
        }),
        ("Формы", new[]
        {
            WireframeKind.Button, WireframeKind.TextField, WireframeKind.TextArea, WireframeKind.Checkbox,
            WireframeKind.Radio, WireframeKind.Select, WireframeKind.Slider, WireframeKind.Calendar
        }),
        ("Данные", new[]
        {
            WireframeKind.List, WireframeKind.DataGrid, WireframeKind.Tree, WireframeKind.Progress,
            WireframeKind.Map, WireframeKind.Video
        }),
        ("Навигация", new[]
        {
            WireframeKind.Tabs, WireframeKind.MenuBar, WireframeKind.Breadcrumbs, WireframeKind.Accordion,
            WireframeKind.StatusBar
        })
    };

    /// <summary>A fresh block of this kind, with its identity in place and its data already started.</summary>
    public static WireframeElement Create(WireframeKind kind)
    {
        var (width, height) = SizeOf(kind);

        var element = new WireframeElement
        {
            Id = Guid.NewGuid().ToString("n"),
            Kind = kind,
            Width = width,
            Height = height,
            FontScale = 1,
            Options = HasRows(kind) ? new List<string>(DefaultOptions) : new List<string>()
        };

        if (kind == WireframeKind.DataGrid)
        {
            element.Columns = new List<string>(DefaultColumns);
            element.Rows = DefaultRows.Select(row => new List<string>(row)).ToList();
        }

        // A bar and a slider sit somewhere inside their range until somebody says otherwise: an empty
        // bar says nothing about what the screen is for.
        if (kind is WireframeKind.Progress or WireframeKind.Slider)
        {
            element.Value = "60";
        }

        TreeState.Normalize(element);
        NavItems.Normalize(element);
        return element;
    }

    /// <summary>Width and height a new block of this kind lands with.</summary>
    public static (double Width, double Height) SizeOf(WireframeKind kind) => kind switch
    {
        WireframeKind.Heading => (240, 40),
        WireframeKind.Text => (200, 24),
        WireframeKind.Button => (120, 40),
        WireframeKind.TextField => (200, 40),
        WireframeKind.TextArea => (240, 96),
        WireframeKind.Checkbox => (180, 24),
        WireframeKind.Radio => (180, 24),
        WireframeKind.Select => (200, 40),
        WireframeKind.List => (240, 140),
        WireframeKind.Image => (200, 140),
        WireframeKind.Divider => (240, 8),
        WireframeKind.Panel => (320, 220),
        WireframeKind.DataGrid => (360, 168),
        WireframeKind.Tabs => (320, 220),
        WireframeKind.MenuBar => (360, 28),
        WireframeKind.Accordion => (280, 144),
        WireframeKind.Tree => (240, 160),
        WireframeKind.Progress => (240, 12),
        WireframeKind.Slider => (240, 20),
        WireframeKind.Badge => (72, 24),
        WireframeKind.Avatar => (40, 40),
        WireframeKind.Calendar => (224, 184),
        WireframeKind.Map => (240, 160),
        WireframeKind.Video => (240, 140),
        WireframeKind.Breadcrumbs => (280, 24),
        WireframeKind.StatusBar => (360, 24),
        _ => (DefaultWidth, DefaultHeight)
    };

    /// <summary>The words a block shows when nobody has written any.</summary>
    public static string PlaceholderFor(WireframeKind kind) => kind switch
    {
        WireframeKind.Heading => "Заголовок",
        WireframeKind.Text => "Текст",
        WireframeKind.Button => "Кнопка",
        WireframeKind.TextField => "Поле ввода",
        WireframeKind.TextArea => "Многострочный текст",
        WireframeKind.Checkbox => "Флажок",
        WireframeKind.Radio => "Переключатель",
        WireframeKind.Select => "Выбор",
        WireframeKind.List => "Список",
        WireframeKind.Image => "Изображение",
        WireframeKind.Badge => "Статус",
        WireframeKind.Avatar => "ИП",
        WireframeKind.Calendar => "Месяц",
        _ => string.Empty
    };

    /// <summary>The name the palette and the inspector show for a kind.</summary>
    public static string NameOf(WireframeKind kind) => kind switch
    {
        WireframeKind.Panel => "Панель",
        WireframeKind.Text => "Текст",
        WireframeKind.Heading => "Заголовок",
        WireframeKind.Button => "Кнопка",
        WireframeKind.TextField => "Поле ввода",
        WireframeKind.TextArea => "Многострочное поле",
        WireframeKind.Checkbox => "Флажок",
        WireframeKind.Radio => "Переключатель",
        WireframeKind.Select => "Выпадающий список",
        WireframeKind.List => "Список",
        WireframeKind.Image => "Изображение",
        WireframeKind.Divider => "Разделитель",
        WireframeKind.DataGrid => "Таблица",
        WireframeKind.Tabs => "Вкладки",
        WireframeKind.MenuBar => "Строка меню",
        WireframeKind.Accordion => "Аккордеон",
        WireframeKind.Tree => "Дерево",
        WireframeKind.Progress => "Прогресс",
        WireframeKind.Slider => "Ползунок",
        WireframeKind.Badge => "Бейдж",
        WireframeKind.Avatar => "Аватар",
        WireframeKind.Calendar => "Календарь",
        WireframeKind.Map => "Карта",
        WireframeKind.Video => "Видео",
        WireframeKind.Breadcrumbs => "Хлебные крошки",
        WireframeKind.StatusBar => "Статус-бар",
        _ => kind.ToString()
    };

    /// <summary>
    /// Splits a row of a list or a dropdown into its label and its value: "Товары = 128" is a row whose
    /// label is "Товары" and whose value is 128. A row without a "=" carries no value, which is the
    /// ordinary case and not a mistake - most rows of a wireframe list are just words.
    ///
    /// One function, three callers: the canvas, the SVG writer and the inspector all have to agree on
    /// what a row means, and this is the only place that decides.
    /// </summary>
    public static (string Label, string Value) SplitOption(string option)
    {
        ArgumentNullException.ThrowIfNull(option);

        var separator = option.IndexOf('=', StringComparison.Ordinal);
        if (separator < 0)
        {
            return (option.Trim(), string.Empty);
        }

        return (option[..separator].Trim(), option[(separator + 1)..].Trim());
    }

    /// <summary>
    /// True when the kind is built out of a list of labels a person writes: the rows of a list, the
    /// tabs of a tab bar, the items of a menu, the sections of an accordion. They share one bag of
    /// options rather than each growing a structure of its own - in a wireframe, every one of them is
    /// the same kind of list.
    /// </summary>
    public static bool HasRows(WireframeKind kind) =>
        kind is WireframeKind.List or WireframeKind.Select or WireframeKind.Tabs or WireframeKind.MenuBar
            or WireframeKind.Accordion or WireframeKind.Tree or WireframeKind.Breadcrumbs
            or WireframeKind.StatusBar;

    /// <summary>True when the kind carries a check state.</summary>
    public static bool HasCheck(WireframeKind kind) => kind is WireframeKind.Checkbox or WireframeKind.Radio;

    /// <summary>True when the block draws a sentence rather than a control.</summary>
    public static bool IsTextual(WireframeKind kind) =>
        kind is WireframeKind.Heading or WireframeKind.Text or WireframeKind.Button
            or WireframeKind.TextField or WireframeKind.TextArea or WireframeKind.Checkbox or WireframeKind.Radio;

    /// <summary>True when the kind has a second line worth writing: a description, a detail, a caption.</summary>
    public static bool AcceptsSecondaryText(WireframeKind kind) =>
        kind is WireframeKind.Text or WireframeKind.Heading or WireframeKind.Panel or WireframeKind.List;

    /// <summary>
    /// True when the kind stands for something a person types, chooses or drags, so a value makes
    /// sense. A bar and a slider read it as a percentage.
    /// </summary>
    public static bool AcceptsValue(WireframeKind kind) =>
        kind is WireframeKind.TextField or WireframeKind.TextArea or WireframeKind.Select
            or WireframeKind.Progress or WireframeKind.Slider;
}
