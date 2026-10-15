using Draftline.App.Model;

namespace Draftline.App.Services;

/// <summary>A visible prototype link in sheet coordinates.</summary>
public sealed record ExportLink(double X, double Y, double Width, double Height, string TargetId, string Url);

/// <summary>PDF hotspots follow the same visible state as the wireframe.</summary>
public static class ExportLinks
{
    /// <summary>Whole-control and per-item destinations, excluding unavailable or hidden items.</summary>
    public static IReadOnlyList<ExportLink> For(DraftLayer layer)
    {
        var links = new List<ExportLink>();
        foreach (var e in TabPanels.Visible(layer))
        {
            if (e.Disabled) continue;
            if (e.HasTransition) { links.Add(new(e.X, e.Y, e.Width, e.Height, e.TargetLayerId, "")); continue; }
            if (e.Kind == WireframeKind.Link && NavItems.SafeUrl(e.Value) is { Length: > 0 } url)
                links.Add(new(e.X, e.Y, e.Width, e.Height, "", url));
            if (!NavItems.Supports(e.Kind)) continue;
            void Add(NavItem item, Rect rect)
            {
                if (item.Disabled || item.Separator || item.Children.Count > 0) return;
                var url = NavItems.SafeUrl(item.Url);
                if (item.TargetLayerId.Length > 0 || url.Length > 0) links.Add(new(rect.X, rect.Y, rect.Width, rect.Height, item.TargetLayerId, url));
            }
            void Rows(IReadOnlyList<NavRow> rows, Rect bounds)
            {
                for (var i = 0; i < NavItems.VisibleCount(e, rows.Count); i++)
                {
                    var height = Math.Min(24, bounds.Height - i * 24);
                    if (height <= 0) break;
                    Add(rows[i].Item, new(bounds.X, bounds.Y + i * 24, bounds.Width, height));
                }
            }
            if (e.Kind == WireframeKind.List) Rows(NavItems.Rows(e.ChoiceItems).ToList(), e.Bounds);
            else if (e.Kind == WireframeKind.Select && ElementState.IsExpanded(e))
                Rows(NavItems.Rows(e.ChoiceItems).ToList(), new(e.X, e.Y + e.Height, e.Width, NavItems.VisibleCount(e, e.ChoiceItems.Count) * 24));
            else if (e.Kind is WireframeKind.MenuBar or WireframeKind.Breadcrumbs or WireframeKind.StatusBar)
            {
                var width = e.Width / Math.Max(1, e.ChoiceItems.Count);
                for (var i = 0; i < e.ChoiceItems.Count; i++) Add(e.ChoiceItems[i], new(e.X + i * width, e.Y, width, e.Height));
                if (e.Kind == WireframeKind.MenuBar && ElementState.IsExpanded(e)) Rows(NavItems.MenuRows(e), NavItems.MenuBounds(e));
            }
        }
        return links;
    }
}
