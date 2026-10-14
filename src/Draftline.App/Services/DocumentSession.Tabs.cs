using Draftline.App.Model;

namespace Draftline.App.Services;

public sealed partial class DocumentSession
{
    /// <summary>Switches the authored panel, keeping hidden blocks out of the selection.</summary>
    public void SelectTab(string id, int index)
    {
        if (CurrentLayer.Find(id) is not { Kind: WireframeKind.Tabs } tabs) return;
        index = Math.Clamp(index, 0, ElementState.Items(tabs).Count - 1);
        if (index != ElementState.Index(tabs))
            Edit("Активная вкладка изменена", _ => tabs.SelectedIndex = index);
        Select(id);
    }

    /// <summary>Attaches a block to a panel or returns it to the sheet.</summary>
    public void AssignToTab(string id, string? parentId, int index)
    {
        if (CurrentLayer.Find(id) is not { } element || element.Kind == WireframeKind.Tabs) return;
        var parent = CurrentLayer.Find(parentId);
        if (parent?.Kind != WireframeKind.Tabs) parent = null;
        index = parent is null ? 0 : Math.Clamp(index, 0, ElementState.Items(parent).Count - 1);
        var nextId = parent?.Id ?? string.Empty;
        if (element.ParentTabsId == nextId && element.ParentTabIndex == index) return;
        Edit("Принадлежность вкладке изменена", _ =>
        {
            element.ParentTabsId = nextId;
            element.ParentTabIndex = index;
            if (parent is not null) parent.SelectedIndex = index;
        });
    }

    /// <summary>Moves a block and every panel member by the same clamped displacement.</summary>
    public void MoveElement(string id, double x, double y)
    {
        if (CurrentLayer.Find(id) is not { } element) return;
        var members = TabPanels.WithChildren(CurrentLayer, new[] { id }).ToList();
        var delta = TabPanels.ClampTranslation(members.Select(e => e.Bounds), x - element.X, y - element.Y,
            CurrentLayer.CanvasWidth, CurrentLayer.CanvasHeight);
        if (delta == (0d, 0d)) return;
        Edit("Положение изменено", _ =>
        {
            foreach (var member in members) { member.X += delta.X; member.Y += delta.Y; }
        });
    }
}
