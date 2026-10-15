namespace Draftline.App.Model;

/// <summary>State rules shared by the inspector, low-fi drawing, preview and SVG export.</summary>
public static class ElementState
{
    /// <summary>Height of one row in an open dropdown, in wireframe pixels.</summary>
    public const double DropdownRowHeight = 24;

    /// <summary>The choices actually drawn, including placeholders for an empty list.</summary>
    public static IReadOnlyList<string> Items(WireframeElement element) =>
        element.Options.Count > 0 ? element.Options : ElementDefaults.DefaultOptions;

    /// <summary>Whether the kind has a selectable item.</summary>
    public static bool HasChoice(WireframeKind kind) => kind is
        WireframeKind.Select or WireframeKind.List or WireframeKind.Tabs or WireframeKind.MenuBar or
        WireframeKind.Accordion or WireframeKind.Tree or WireframeKind.CoverFlow or WireframeKind.Alert;

    /// <summary>Whether the kind can reveal more content.</summary>
    public static bool HasDisclosure(WireframeKind kind) => kind is
        WireframeKind.Select or WireframeKind.Accordion or WireframeKind.MenuBar or WireframeKind.Tooltip or WireframeKind.Popover;

    /// <summary>Whether availability is meaningful for this control.</summary>
    public static bool HasAvailability(WireframeKind kind) => HasChoice(kind) || ExtraElements.Is(kind) || kind is
        WireframeKind.Button or WireframeKind.TextField or WireframeKind.TextArea or
        WireframeKind.Checkbox or WireframeKind.Radio or WireframeKind.Slider or WireframeKind.DataGrid or WireframeKind.Calendar;

    /// <summary>Effective selected item, with a first-tab default for old files.</summary>
    public static int Index(WireframeElement element) =>
        element.SelectedIndex < 0 ? (element.Kind == WireframeKind.Tabs ? 0 : -1)
            : Math.Clamp(element.SelectedIndex, 0, Items(element).Count - 1);

    /// <summary>Effective disclosure state; old trees retain their visible child rows.</summary>
    public static bool IsExpanded(WireframeElement element) =>
        element.Expanded ?? element.Kind == WireframeKind.Tree;

    /// <summary>Label of the chosen option, or the block's free-form value/hint.</summary>
    public static string DisplayChoice(WireframeElement element) => Index(element) is var index && index >= 0
        ? ElementDefaults.SplitOption(Items(element)[index]).Label : element.DisplayValue;

    /// <summary>Whole drawing height, including the menu outside a dropdown's control rectangle.</summary>
    public static double DrawingHeight(WireframeElement element) => element.Height +
        (element.Kind == WireframeKind.Select && IsExpanded(element)
            ? NavItems.VisibleCount(element, Items(element).Count) * DropdownRowHeight : element.Kind == WireframeKind.MenuBar && IsExpanded(element) ? NavItems.MenuBounds(element).Height : 0);

    /// <summary>Keeps choice indices valid when importing or changing the list of items.</summary>
    public static void Normalize(WireframeElement element)
    {
        element.SelectedIndex = HasChoice(element.Kind)
            ? Math.Clamp(element.SelectedIndex, -1, Items(element).Count - 1) : -1;
    }
}
