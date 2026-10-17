namespace Draftline.App.Model;

/// <summary>Document defaults and import rules for metrics and page headers.</summary>
public static class SummaryBlocks
{
    /// <summary>Whether the kind describes a metric or the top of a page.</summary>
    public static bool Is(WireframeKind kind) => kind is WireframeKind.Stat or WireframeKind.PageHeader;

    /// <summary>The visible low-fidelity action bounds, also used by SVG/PDF export.</summary>
    public static Rect ActionBounds(WireframeElement element)
    {
        var size = Math.Max(1, Math.Round(12 * element.FontScale, 1));
        return new(element.X + 12, element.Y + 104,
            Math.Min(Math.Max(0, element.Width - 24), Math.Max(88, element.HeaderAction.Length * size * .6 + 24)),
            Math.Clamp(element.Height - 104, 0, 32));
    }

    /// <summary>Starts a new block with an editable example.</summary>
    public static void Initialize(WireframeElement element)
    {
        if (element.Kind == WireframeKind.Stat)
        {
            element.Value = "128 000";
            element.StatUnit = "₽";
            element.StatDelta = "+12%";
            element.StatTone = "success";
            element.SecondaryText = "За этот месяц";
        }
        else if (element.Kind == WireframeKind.PageHeader)
        {
            element.HeaderEyebrow = "Рабочая область";
            element.SecondaryText = "Основные сведения и действия";
            element.HeaderAction = "Создать";
        }
    }

    /// <summary>Keeps missing imported fields and semantic values safe.</summary>
    public static void Normalize(WireframeElement element)
    {
        element.StatUnit ??= "";
        element.StatDelta ??= "";
        element.StatTone = element.StatTone is "success" or "warning" or "error" or "info" ? element.StatTone : "default";
        element.HeaderEyebrow ??= "";
        element.HeaderAction ??= "";
        element.HeadingLevel = Math.Clamp(element.HeadingLevel, 1, 6);
    }
}
