namespace Draftline.App.Model;
/// <summary>Nested group paths, independent of tab-panel membership.</summary>
public static class ElementGroups
{
    /// <summary>Group immediately inside the current editing scope.</summary>
    public static string Next(WireframeElement e, string scope)
    {
        var index = scope.Length == 0 ? -1 : e.GroupPath.IndexOf(scope);
        return index < -1 || (scope.Length > 0 && index < 0) ? "" : e.GroupPath.ElementAtOrDefault(index + 1) ?? "";
    }

    /// <summary>Members of any group selected at the current scope.</summary>
    public static IReadOnlyList<string> Expand(DraftLayer layer, IEnumerable<string> ids, string scope)
    {
        var selected = ids.ToHashSet(StringComparer.Ordinal);
        var groups = layer.Elements.Where(e => selected.Contains(e.Id)).Select(e => Next(e, scope)).Where(id => id.Length > 0).ToHashSet();
        return layer.Elements.Where(e => selected.Contains(e.Id) || e.GroupPath.Any(groups.Contains)).Select(e => e.Id).ToList();
    }

    /// <summary>Bounding rectangle of a nonempty collection.</summary>
    public static Rect Bounds(IEnumerable<Rect> bounds)
    {
        var list = bounds.ToList();
        return list.Count == 0 ? new(0, 0, 0, 0) : new(list.Min(r => r.X), list.Min(r => r.Y), list.Max(r => r.Right) - list.Min(r => r.X), list.Max(r => r.Bottom) - list.Min(r => r.Y));
    }

    /// <summary>Safe, acyclic paths; a singleton is no longer a group.</summary>
    public static void Normalize(DraftLayer layer)
    {
        foreach (var e in layer.Elements)
            e.GroupPath = (e.GroupPath ?? new()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).Take(16).ToList();
        var prefixes = new Dictionary<string, string[]>();
        foreach (var e in layer.Elements)
            for (var index = 0; index < e.GroupPath.Count; index++)
            {
                var group = e.GroupPath[index];
                var prefix = e.GroupPath.Take(index).ToArray();
                if (prefixes.TryGetValue(group, out var known) && !known.SequenceEqual(prefix))
                {
                    e.GroupPath.RemoveRange(index, e.GroupPath.Count - index);
                    break;
                }

                prefixes[group] = prefix;
            }

        var singles = layer.Elements.SelectMany(e => e.GroupPath).GroupBy(id => id).Where(group => group.Count() < 2).Select(group => group.Key).ToHashSet();
        foreach (var e in layer.Elements)
            e.GroupPath.RemoveAll(singles.Contains);
    }
}
