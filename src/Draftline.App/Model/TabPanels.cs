namespace Draftline.App.Model;

/// <summary>Tab panel membership, visibility and content, shared by editor, preview and export.</summary>
public static class TabPanels
{
    /// <summary>Height of the header strip on a tab group.</summary>
    public const double HeaderHeight = 32;

    /// <summary>Whether this kind owns independently visible panels.</summary>
    public static bool IsContainer(WireframeKind kind) => kind is WireframeKind.Tabs or WireframeKind.Accordion;

    /// <summary>Effective panel index, including a closed accordion's editable first section.</summary>
    public static int Index(WireframeElement element) => Math.Max(0, ElementState.Index(element));

    /// <summary>Absolute panel rectangle, shared by palette placement and export.</summary>
    public static Rect PanelBounds(WireframeElement element) => element.Kind == WireframeKind.Accordion
        ? new(element.X, element.Y + (Index(element) + 1) * 28, element.Width,
            Math.Max(0, element.Height - ElementState.Items(element).Count * 28))
        : new(element.X, element.Y + HeaderHeight, element.Width, Math.Max(0, element.Height - HeaderHeight));

    /// <summary>The text of the active panel, empty when no text was authored.</summary>
    public static string Content(WireframeElement tabs) =>
        tabs.TabContents.ElementAtOrDefault(Index(tabs)) ?? string.Empty;

    /// <summary>Stores content without discarding panels temporarily removed from the tab list.</summary>
    public static void SetContent(WireframeElement tabs, string text)
    {
        var index = Index(tabs);
        while (tabs.TabContents.Count <= index) tabs.TabContents.Add(string.Empty);
        tabs.TabContents[index] = text;
    }

    /// <summary>A valid parent group on the same sheet; nested tab groups are deliberately excluded.</summary>
    public static WireframeElement? Parent(DraftLayer layer, WireframeElement element) =>
        !IsContainer(element.Kind) && layer.Find(element.ParentTabsId) is { } parent && IsContainer(parent.Kind)
            ? parent : null;

    /// <summary>Whether the element belongs to the selected panel. Preview supplies its own state copies.</summary>
    public static bool IsVisible(DraftLayer layer, WireframeElement element,
        Func<WireframeElement, WireframeElement>? state = null) =>
        !element.Hidden && (Parent(layer, element) is not { } parent || (!parent.Hidden &&
        (element.ParentTabIndex == Index(state?.Invoke(parent) ?? parent) &&
         (parent.Kind != WireframeKind.Accordion || ElementState.IsExpanded(state?.Invoke(parent) ?? parent)))));

    /// <summary>Visible elements, keeping each panel behind its children regardless of local order.</summary>
    public static IEnumerable<WireframeElement> Visible(DraftLayer layer,
        Func<WireframeElement, WireframeElement>? state = null) => layer.Elements
        .Where(element => IsVisible(layer, element, state))
        .OrderBy(element => Parent(layer, element)?.ZIndex ?? element.ZIndex)
        .ThenBy(element => Parent(layer, element) is null ? 0 : 1)
        .ThenBy(element => element.ZIndex);

    /// <summary>Includes children of selected groups in moving, copying and deleting.</summary>
    public static IEnumerable<WireframeElement> WithChildren(DraftLayer layer, IEnumerable<string> ids)
    {
        var selected = ids.ToHashSet(StringComparer.Ordinal);
        return layer.Elements.Where(element => selected.Contains(element.Id) ||
            (Parent(layer, element) is { } parent && selected.Contains(parent.Id)));
    }

    /// <summary>Clamps one translation for the whole group, so children cannot drift at sheet edges.</summary>
    public static (double X, double Y) ClampTranslation(IEnumerable<Rect> bounds, double dx, double dy,
        double width, double height)
    {
        var rectangles = bounds.ToList();
        if (rectangles.Count == 0) return (0, 0);
        return (Math.Clamp(dx, -rectangles.Min(r => r.X), Math.Max(-rectangles.Min(r => r.X), width - rectangles.Max(r => r.Right))),
            Math.Clamp(dy, -rectangles.Min(r => r.Y), Math.Max(-rectangles.Min(r => r.Y), height - rectangles.Max(r => r.Bottom))));
    }

    /// <summary>Sanitizes imported content and membership without deleting text or blocks.</summary>
    public static void Normalize(DraftLayer layer)
    {
        foreach (var element in layer.Elements)
        {
            element.TabContents ??= new();
            if (element.Kind == WireframeKind.Accordion && element.TabContents.Count == 0 && !string.IsNullOrEmpty(element.SecondaryText))
                element.TabContents = Enumerable.Repeat(element.SecondaryText, ElementState.Items(element).Count).ToList();
            for (var i = 0; i < element.TabContents.Count; i++) element.TabContents[i] ??= string.Empty;
            element.ParentTabsId ??= string.Empty;
            if (Parent(layer, element) is { } parent)
                element.ParentTabIndex = Math.Clamp(element.ParentTabIndex, 0, ElementState.Items(parent).Count - 1);
            else
            {
                element.ParentTabsId = string.Empty;
                element.ParentTabIndex = 0;
            }
        }
    }
}
