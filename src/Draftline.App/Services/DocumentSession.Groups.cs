using Draftline.App.Model;

namespace Draftline.App.Services;

public sealed partial class DocumentSession
{
    /// <summary>The group currently open for individual editing; never saved with the document.</summary>
    public string EditingGroupId { get; private set; } = "";
    /// <summary>All operation members, including groups and inactive tab children.</summary>
    public IReadOnlyList<WireframeElement> Members(IEnumerable<string>? ids = null) =>
        TabPanels.WithChildren(CurrentLayer,ElementGroups.Expand(CurrentLayer,ids??Selection,EditingGroupId)).ToList();
    /// <summary>Visible selection bounds; a group has one enclosing frame.</summary>
    public Rect? SelectionBounds => Selection.Count==0 ? null : Selected?.Bounds ?? ElementGroups.Bounds(Members().Select(e=>e.Bounds));
    /// <summary>Selected group at the current editing scope, if it is the only one.</summary>
    public string SelectedGroupId
    {
        get { var groups=Selection.Select(id=>CurrentLayer.Find(id)).Where(e=>e is not null).Select(e=>ElementGroups.Next(e!,EditingGroupId)).Distinct().ToList();return groups.Count==1?groups[0]:""; }
    }
    /// <summary>Create a nested group in one history step.</summary>
    public void GroupSelected()
    {
        if(Selection.Count<2)return;
        var members=Members();var id=Guid.NewGuid().ToString("n");
        if(members.Any(e=>e.GroupPath.Count>=16))return;
        Edit("Блоки сгруппированы",_=>
        {
            foreach(var e in members){var index=EditingGroupId.Length==0?0:e.GroupPath.IndexOf(EditingGroupId)+1;e.GroupPath.Insert(Math.Max(0,index),id);}
        });
        SetSelection(members.Select(e=>e.Id));
    }
    /// <summary>Remove the selected level while preserving its nested groups.</summary>
    public void UngroupSelected()
    {
        var groups=Selection.Select(id=>CurrentLayer.Find(id)).Where(e=>e is not null).Select(e=>ElementGroups.Next(e!,EditingGroupId)).Where(id=>id.Length>0).ToHashSet();
        if(groups.Count==0)return;
        var selected=Selection.ToArray();Edit("Группа разобрана",_=>{foreach(var e in CurrentLayer.Elements)e.GroupPath.RemoveAll(groups.Contains);});SetSelection(selected);
    }
    /// <summary>Enter the selected group to edit its members.</summary>
    public bool EnterGroup(string? elementId = null)
    {
        if(elementId is not null && EditingGroupId.Length>0 && CurrentLayer.Find(elementId) is { } outside && !outside.GroupPath.Contains(EditingGroupId)) EditingGroupId="";
        var group=elementId is null?SelectedGroupId:CurrentLayer.Find(elementId) is { } e?ElementGroups.Next(e,EditingGroupId):"";
        if(group.Length==0)return false;
        EditingGroupId=group;_selection.Clear();Changed?.Invoke();return true;
    }
    /// <summary>Return to the containing group or sheet.</summary>
    public bool LeaveGroup()
    {
        if(EditingGroupId.Length==0)return false;
        var path=CurrentLayer.Elements.FirstOrDefault(e=>e.GroupPath.Contains(EditingGroupId))?.GroupPath;
        var index=path?.IndexOf(EditingGroupId)??-1;var old=EditingGroupId;
        EditingGroupId=index>0?path![index-1]:"";
        SetSelection(CurrentLayer.Elements.Where(e=>e.GroupPath.Contains(old)).Select(e=>e.Id));Changed?.Invoke();return true;
    }
    /// <summary>Clone operation members with independent identities and nested groups.</summary>
    private List<WireframeElement> CloneMembers(IReadOnlyList<WireframeElement> members, bool detach = false)
    {
        var ids=members.ToDictionary(e=>e.Id,_=>Guid.NewGuid().ToString("n"));var groups=new Dictionary<string,string>();
        var copies=new List<WireframeElement>();
        foreach(var e in members)
        {
            var copy=e.Clone();copy.Id=ids[e.Id];
            copy.ParentTabsId=ids.TryGetValue(e.ParentTabsId,out var parent)?parent:detach?"":e.ParentTabsId;
            if(copy.ParentTabsId.Length==0)copy.ParentTabIndex=0;
            var prefix=EditingGroupId.Length==0?0:e.GroupPath.IndexOf(EditingGroupId)+1;
            copy.GroupPath=new();
            for(var i=0;i<e.GroupPath.Count;i++)
            {
                var group=e.GroupPath[i];
                if(i<prefix){if(!detach)copy.GroupPath.Add(group);continue;}
                if(!groups.TryGetValue(group,out var next))groups[group]=next=Guid.NewGuid().ToString("n");
                copy.GroupPath.Add(next);
            }
            copies.Add(copy);
        }
        return copies;
    }
    /// <summary>Align a selection as one object without changing its internal spacing.</summary>
    public void AlignSelection(ElementAlignment alignment)
    {
        if(!CanTransformSelection || SelectionBounds is not { } bounds)return;
        var members=Members();var next=CanvasGeometry.Align(bounds,alignment,Document.GridSize,CurrentLayer.CanvasWidth,CurrentLayer.CanvasHeight);
        var delta=TabPanels.ClampTranslation(members.Select(e=>e.Bounds),next.X-bounds.X,next.Y-bounds.Y,CurrentLayer.CanvasWidth,CurrentLayer.CanvasHeight);
        Edit("Выделение выровнено",_=>{foreach(var e in members){e.X+=delta.X;e.Y+=delta.Y;}});
    }
}
