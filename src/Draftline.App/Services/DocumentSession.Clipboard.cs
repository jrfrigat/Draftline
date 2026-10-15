using Draftline.App.Model;

namespace Draftline.App.Services;

public sealed partial class DocumentSession
{
    private List<WireframeElement> _clipboard = new();
    private int _pasteCount;
    /// <summary>The editor's private clipboard holds no browser permission or shared references.</summary>
    public bool CanPaste => _clipboard.Count > 0;
    /// <summary>Snapshot selected groups and tab children, detached from their enclosing context.</summary>
    public bool CopySelection()
    {
        var members=Members();if(members.Count==0)return false;
        _clipboard=CloneMembers(members,detach:true);_pasteCount=0;
        Status=$"Скопировано блоков: {_clipboard.Count}";Changed?.Invoke();return true;
    }
    /// <summary>Copy and remove in one undoable deletion.</summary>
    public bool CutSelection() => CopySelection() && RemoveSelected();
    /// <summary>Paste fresh identities in one undo step, keeping relative positions inside the sheet.</summary>
    public bool PasteSelection()
    {
        if(!CanPaste)return false;
        var copies=CloneMembers(_clipboard,detach:true);
        var bounds=ElementGroups.Bounds(copies.Select(e=>e.Bounds));
        var scale=Math.Min(1,Math.Min(CurrentLayer.CanvasWidth/Math.Max(1,bounds.Width),CurrentLayer.CanvasHeight/Math.Max(1,bounds.Height)));
        var prefix=CurrentLayer.Elements.FirstOrDefault(e=>e.GroupPath.Contains(EditingGroupId) && EditingGroupId.Length>0)?.GroupPath;
        var prefixLength=prefix is null?0:prefix.IndexOf(EditingGroupId)+1;
        foreach(var e in copies)
        {
            e.X=bounds.X+(e.X-bounds.X)*scale;e.Y=bounds.Y+(e.Y-bounds.Y)*scale;
            e.Width=Math.Max(CanvasGeometry.MinimumSize,e.Width*scale);e.Height=Math.Max(CanvasGeometry.MinimumSize,e.Height*scale);
            if(prefixLength>0)e.GroupPath.InsertRange(0,prefix!.Take(prefixLength));
        }
        var offset=(Document.GridSize>1?Document.GridSize:8)*++_pasteCount;
        var delta=TabPanels.ClampTranslation(copies.Select(e=>e.Bounds),offset,offset,CurrentLayer.CanvasWidth,CurrentLayer.CanvasHeight);
        Edit($"Вставлено блоков: {copies.Count}",_=>
        {
            foreach(var e in copies)
            {
                var rect=CanvasGeometry.Clamp(e.Bounds.With(x:e.X+delta.X,y:e.Y+delta.Y),CurrentLayer.CanvasWidth,CurrentLayer.CanvasHeight);
                (e.X,e.Y,e.Width,e.Height)=(rect.X,rect.Y,rect.Width,rect.Height);
                e.ZIndex=CurrentLayer.NextZIndex();CurrentLayer.Elements.Add(e);
            }
        });
        SetSelection(copies.Select(e=>e.Id));return true;
    }
}
