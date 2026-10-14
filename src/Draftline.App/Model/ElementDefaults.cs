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

    /// <summary>A fresh block of this kind, with its identity already in place.</summary>
    public static WireframeElement Create(WireframeKind kind)
    {
        var (width, height) = SizeOf(kind);

        return new WireframeElement
        {
            Id = Guid.NewGuid().ToString("n"),
            Kind = kind,
            Width = width,
            Height = height,
            FontScale = 1,
            Options = HasRows(kind) ? new List<string>(DefaultOptions) : new List<string>()
        };
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

    /// <summary>True when the kind carries a list of rows a person can edit.</summary>
    public static bool HasRows(WireframeKind kind) => kind is WireframeKind.List or WireframeKind.Select;

    /// <summary>True when the kind carries a check state.</summary>
    public static bool HasCheck(WireframeKind kind) => kind is WireframeKind.Checkbox or WireframeKind.Radio;

    /// <summary>True when the block draws a sentence rather than a control.</summary>
    public static bool IsTextual(WireframeKind kind) =>
        kind is WireframeKind.Heading or WireframeKind.Text or WireframeKind.Button
            or WireframeKind.TextField or WireframeKind.TextArea or WireframeKind.Checkbox or WireframeKind.Radio;

    /// <summary>True when the kind has a second line worth writing: a description, a detail, a caption.</summary>
    public static bool AcceptsSecondaryText(WireframeKind kind) =>
        kind is WireframeKind.Text or WireframeKind.Heading or WireframeKind.Panel or WireframeKind.List;

    /// <summary>True when the kind stands for something a person types or chooses, so a value makes sense.</summary>
    public static bool AcceptsValue(WireframeKind kind) =>
        kind is WireframeKind.TextField or WireframeKind.TextArea or WireframeKind.Select;
}
