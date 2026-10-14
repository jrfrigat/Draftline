namespace Draftline.App.Model;

/// <summary>A choice or navigation item with its own state, action and submenu.</summary>
public sealed class NavItem
{
    /// <summary>Visible label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Right-aligned value or keyboard shortcut.</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Unavailable item.</summary>
    public bool Disabled { get; set; }
    /// <summary>A separator instead of a selectable item.</summary>
    public bool Separator { get; set; }
    /// <summary>Whether this submenu is expanded.</summary>
    public bool Expanded { get; set; }
    /// <summary>Destination screen; empty means no screen action.</summary>
    public string TargetLayerId { get; set; } = string.Empty;
    /// <summary>Opens the target as a dialog.</summary>
    public bool AsDialog { get; set; }
    /// <summary>External http(s) URL, mutually exclusive with the screen destination.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>Child menu items.</summary>
    public List<NavItem> Children { get; set; } = new();
    /// <summary>Independent copy for history and preview.</summary>
    public NavItem Clone() => new() { Label = Label, Value = Value, Disabled = Disabled, Separator = Separator,
        Expanded = Expanded, TargetLayerId = TargetLayerId, AsDialog = AsDialog, Url = Url,
        Children = Children.Select(child => child.Clone()).ToList() };
}

/// <summary>A row and its stable path within a control.</summary>
public sealed record NavRow(string Path, NavItem Item, int Depth);

/// <summary>Choice metadata, hierarchy, bounds and safe links shared by UI and SVG.</summary>
public static class NavItems
{
    /// <summary>Kinds that support per-item metadata.</summary>
    public static bool Supports(WireframeKind kind) => kind is WireframeKind.Select or WireframeKind.List or
        WireframeKind.MenuBar or WireframeKind.Breadcrumbs or WireframeKind.StatusBar;
    /// <summary>Only ordinary web destinations can become prototype links.</summary>
    public static string SafeUrl(string? value) => Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" ? uri.AbsoluteUri : string.Empty;
    /// <summary>All rows, including closed children when editing properties.</summary>
    public static IEnumerable<NavRow> Rows(IEnumerable<NavItem> items, string prefix = "", int depth = 0, bool visibleOnly = false)
    {
        var index = 0;
        foreach (var item in items)
        {
            var path = prefix.Length == 0 ? index.ToString() : prefix + "/" + index;
            yield return new(path, item, depth);
            if (depth < 16 && (!visibleOnly || item.Expanded))
                foreach (var child in Rows(item.Children, path, depth + 1, visibleOnly)) yield return child;
            index++;
        }
    }
    /// <summary>Item at a slash-separated index path.</summary>
    public static NavItem? Find(WireframeElement element, string path)
    {
        var items = element.ChoiceItems;
        NavItem? item = null;
        foreach (var part in path.Split('/'))
        {
            if (!int.TryParse(part, out var index) || index < 0 || index >= items.Count) return null;
            item = items[index]; items = item.Children;
        }
        return item;
    }
    /// <summary>Visible rows in the authored submenu.</summary>
    public static IReadOnlyList<NavRow> MenuRows(WireframeElement element)
    {
        var index = Math.Max(0, ElementState.Index(element));
        return index < element.ChoiceItems.Count ? Rows(element.ChoiceItems[index].Children, index.ToString(), visibleOnly: true).ToList() : new List<NavRow>();
    }
    /// <summary>Maximum number of painted rows; the canvas scrolls the rest.</summary>
    public static int VisibleCount(WireframeElement element, int count) => element.MaxVisibleItems > 0 ? Math.Min(count, element.MaxVisibleItems) : count;
    /// <summary>Popup bounds inside the control's width.</summary>
    public static Rect MenuBounds(WireframeElement element)
    {
        var width = Math.Min(element.Width, Math.Max(180, element.Width / Math.Max(1, element.ChoiceItems.Count)));
        var left = Math.Min(Math.Max(0, ElementState.Index(element)) * element.Width / Math.Max(1, element.ChoiceItems.Count), element.Width - width);
        return new(element.X + left, element.Y + element.Height, width, VisibleCount(element, MenuRows(element).Count) * 24);
    }
    /// <summary>Parses nested submenu text, retaining metadata for matching labels.</summary>
    public static List<NavItem> Parse(string text, IEnumerable<NavItem> previous)
    {
        var old = Rows(previous).GroupBy(row => row.Item.Label).ToDictionary(group => group.Key, group => new Queue<NavItem>(group.Select(row => row.Item)));
        var roots = new List<NavItem>(); var stack = new List<List<NavItem>> { roots }; NavItem? last = null;
        foreach (var line in text.Replace("\t", "  ", StringComparison.Ordinal).Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            var depth = Math.Min(16, (line.Length - line.TrimStart().Length) / 2);
            depth = Math.Min(depth, stack.Count);
            while (stack.Count > depth + 1) stack.RemoveAt(stack.Count - 1);
            if (stack.Count <= depth && last is not null) stack.Add(last.Children);
            var (label, value) = ElementDefaults.SplitOption(line.Trim());
            var item = old.TryGetValue(label, out var matches) && matches.Count > 0 ? matches.Dequeue().Clone() : new NavItem();
            item.Label = label; item.Value = value; item.Separator = label == "---"; item.Children = new();
            stack[^1].Add(item); last = item;
        }
        return roots;
    }
    /// <summary>Editable submenu structure.</summary>
    public static string Structure(IEnumerable<NavItem> items) => string.Join('\n', Rows(items).Select(row =>
        new string(' ', row.Depth * 2) + row.Item.Label + (row.Item.Value.Length > 0 ? " = " + row.Item.Value : "")));
    /// <summary>Aligns old Options with per-item metadata and sanitizes imported hierarchy.</summary>
    public static void Normalize(WireframeElement element)
    {
        element.ChoiceItems ??= new(); element.SelectedPath ??= string.Empty;
        element.MaxVisibleItems = Math.Clamp(element.MaxVisibleItems, 0, 100);
        if (!Supports(element.Kind)) return;
        var labels = ElementState.Items(element);
        while (element.ChoiceItems.Count < labels.Count) element.ChoiceItems.Add(new());
        if (element.ChoiceItems.Count > labels.Count) element.ChoiceItems.RemoveRange(labels.Count, element.ChoiceItems.Count - labels.Count);
        for (var i = 0; i < labels.Count; i++)
        {
            element.ChoiceItems[i] ??= new();
            (element.ChoiceItems[i].Label, element.ChoiceItems[i].Value) = ElementDefaults.SplitOption(labels[i]);
            if (element.ChoiceItems[i].Label == "---") element.ChoiceItems[i].Separator = true;
        }
        NormalizeChildren(element.ChoiceItems, 0);
        if (Find(element, element.SelectedPath) is null) element.SelectedPath = string.Empty;
    }
    private static void NormalizeChildren(List<NavItem> items, int depth)
    {
        items.RemoveAll(item => item is null);
        foreach (var item in items)
        {
            item.Label ??= string.Empty; item.Value ??= string.Empty; item.TargetLayerId ??= string.Empty;
            item.Url = SafeUrl(item.Url); if (item.Url.Length > 0) item.TargetLayerId = string.Empty;
            item.Children ??= new(); if (depth >= 16) item.Children.Clear(); else NormalizeChildren(item.Children, depth + 1);
        }
    }
}
