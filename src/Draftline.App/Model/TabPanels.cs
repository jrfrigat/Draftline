namespace Draftline.App.Model;

/// <summary>Tab panel membership, visibility and content, shared by editor, preview and export.</summary>
public static class TabPanels
{
    /// <summary>Height of the header strip on a tab group.</summary>
    public const double HeaderHeight = 32;

    /// <summary>The text of the active panel, empty when no text was authored.</summary>
    public static string Content(WireframeElement tabs) =>
        tabs.TabContents.ElementAtOrDefault(ElementState.Index(tabs)) ?? string.Empty;

    /// <summary>Stores content without discarding panels temporarily removed from the tab list.</summary>
    public static void SetContent(WireframeElement tabs, string text)
    {
        var index = ElementState.Index(tabs);
        while (tabs.TabContents.Count <= index) tabs.TabContents.Add(string.Empty);
        tabs.TabContents[index] = text;
    }

    /// <summary>A valid parent group on the same sheet; nested tab groups are deliberately excluded.</summary>
    public static WireframeElement? Parent(DraftLayer layer, WireframeElement element) =>
        element.Kind != WireframeKind.Tabs && layer.Find(element.ParentTabsId) is { Kind: WireframeKind.Tabs } parent
            ? parent : null;

    /// <summary>Whether the element belongs to the selected panel. Preview supplies its own state copies.</summary>
    public static bool IsVisible(DraftLayer layer, WireframeElement element,
        Func<WireframeElement, WireframeElement>? state = null) =>
        Parent(layer, element) is not { } parent ||
        element.ParentTabIndex == ElementState.Index(state?.Invoke(parent) ?? parent);

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
