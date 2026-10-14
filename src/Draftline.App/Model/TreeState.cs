namespace Draftline.App.Model;

/// <summary>Flat tree structure and disclosure, shared by every drawing and editor.</summary>
public static class TreeState
{
    /// <summary>Whether the row has child rows.</summary>
    public static bool IsBranch(WireframeElement tree, int index) => index + 1 < tree.TreeDepths.Count &&
        tree.TreeDepths[index + 1] > tree.TreeDepths[index];

    /// <summary>Parent row, or -1 for a root.</summary>
    public static int Parent(WireframeElement tree, int index)
    {
        for (var i = index - 1; i >= 0; i--)
            if (tree.TreeDepths[i] < tree.TreeDepths[index]) return i;
        return -1;
    }

    /// <summary>Rows whose entire ancestor chain is expanded.</summary>
    public static IEnumerable<int> Visible(WireframeElement tree)
    {
        for (var i = 0; i < tree.TreeDepths.Count; i++)
        {
            var visible = true;
            for (var parent = Parent(tree, i); parent >= 0; parent = Parent(tree, parent))
                if (!tree.ExpandedNodes.Contains(parent)) { visible = false; break; }
            if (visible) yield return i;
        }
    }

    /// <summary>Selects a row and toggles only that branch.</summary>
    public static void Choose(WireframeElement tree, int index)
    {
        if (index < 0 || index >= tree.TreeDepths.Count) return;
        tree.SelectedIndex = index;
        if (IsBranch(tree, index) && !tree.ExpandedNodes.Remove(index)) tree.ExpandedNodes.Add(index);
    }

    /// <summary>Reveals the selected row when it is chosen from the inspector.</summary>
    public static void Reveal(WireframeElement tree, int index)
    {
        if (index < 0 || index >= tree.TreeDepths.Count) return;
        for (var parent = Parent(tree, index); parent >= 0; parent = Parent(tree, parent))
            if (!tree.ExpandedNodes.Contains(parent)) tree.ExpandedNodes.Add(parent);
    }

    /// <summary>Text structure using two spaces per level.</summary>
    public static string Structure(WireframeElement tree) => string.Join('\n',
        ElementState.Items(tree).Select((text, i) => new string(' ', tree.TreeDepths.ElementAtOrDefault(i) * 2) + text));

    /// <summary>Reads indentation without losing the labels.</summary>
    public static void Parse(WireframeElement tree, string text)
    {
        var rows = text.Replace("\t", "  ", StringComparison.Ordinal).Split('\n').Where(row => !string.IsNullOrWhiteSpace(row)).ToList();
        tree.Options = rows.Select(row => row.Trim()).ToList();
        tree.TreeDepths = rows.Select(row => (row.Length - row.TrimStart().Length) / 2).ToList();
        Normalize(tree);
    }

    /// <summary>Normalizes depths and branch indices, including the old root-plus-children tree.</summary>
    public static void Normalize(WireframeElement tree)
    {
        tree.TreeDepths ??= new(); tree.ExpandedNodes ??= new();
        if (tree.Kind != WireframeKind.Tree) return;
        var count = ElementState.Items(tree).Count;
        if (tree.TreeDepths.Count == 0)
        {
            tree.TreeDepths = Enumerable.Range(0, count).Select(i => i == 0 ? 0 : 1).ToList();
            if (ElementState.IsExpanded(tree)) tree.ExpandedNodes.Add(0);
        }
        while (tree.TreeDepths.Count < count) tree.TreeDepths.Add(0);
        if (tree.TreeDepths.Count > count) tree.TreeDepths.RemoveRange(count, tree.TreeDepths.Count - count);
        for (var i = 0; i < count; i++) tree.TreeDepths[i] = i == 0 ? 0 : Math.Clamp(tree.TreeDepths[i], 0, Math.Min(32, tree.TreeDepths[i - 1] + 1));
        tree.ExpandedNodes = tree.ExpandedNodes.Where(i => i >= 0 && i < count && IsBranch(tree, i)).Distinct().Order().ToList();
        ElementState.Normalize(tree);
    }
}
