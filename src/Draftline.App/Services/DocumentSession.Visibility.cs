using Draftline.App.Model;

namespace Draftline.App.Services;

public sealed partial class DocumentSession
{
    /// <summary>A group containing a locked member keeps its relative geometry.</summary>
    public bool CanTransformSelection => Selection.Count > 0 && Members().All(e => !e.Locked);

    /// <summary>Protect or release all selected operation members in one undo step.</summary>
    public void LockSelection(bool value) => SetLocked(Selection, value);
    public void SetLocked(IEnumerable<string> ids, bool value)
    {
        var members = Members(ids).Where(e => e.Locked != value).ToList();
        if (members.Count == 0) return;
        Edit(value ? "Положение заблокировано" : "Положение разблокировано", _ => members.ForEach(e => e.Locked = value));
    }

    /// <summary>Hide operation members without removing them from the document.</summary>
    public void HideSelection() => SetHidden(Selection, true);
    public void SetHidden(IEnumerable<string> ids, bool value)
    {
        var members = Members(ids).Where(e => e.Hidden != value).ToList();
        if (members.Count == 0) return;
        Edit(value ? "Блоки скрыты" : "Блоки показаны", _ => members.ForEach(e => e.Hidden = value));
    }

    /// <summary>Reveal an individual block, including its inactive panel and containing group.</summary>
    public void RevealElement(string id)
    {
        if (CurrentLayer.Find(id) is not { } element) return;
        var parent = TabPanels.Parent(CurrentLayer, element);
        if (element.Hidden || parent is not null && (parent.Hidden || TabPanels.Index(parent) != element.ParentTabIndex ||
            parent.Kind == WireframeKind.Accordion && !ElementState.IsExpanded(parent)))
            Edit("Блок показан", _ =>
            {
                element.Hidden = false;
                if (parent is not null) { parent.Hidden = false; parent.SelectedIndex = element.ParentTabIndex; parent.Expanded = true; }
            });
        EditingGroupId = element.GroupPath.LastOrDefault() ?? "";
        Select(id);
    }
}
