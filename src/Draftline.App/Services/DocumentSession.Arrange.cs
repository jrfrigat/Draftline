using Draftline.App.Model;

namespace Draftline.App.Services;

public sealed partial class DocumentSession
{
    private List<List<WireframeElement>> ArrangementUnits()
    {
        var pending = Members().ToList(); var units = new List<List<WireframeElement>>();
        while (pending.Count > 0)
        {
            var first = pending.FirstOrDefault(e=>TabPanels.Parent(CurrentLayer,e) is null) ?? pending[0];
            var ids = Members(new[]{first.Id}).Select(e=>e.Id).ToHashSet();
            var unit = pending.Where(e=>ids.Contains(e.Id)).ToList(); units.Add(unit); pending.RemoveAll(e=>ids.Contains(e.Id));
        }
        return units;
    }
    public bool CanDistribute => CanTransformSelection && ArrangementUnits().Count >= 3;
    /// <summary>Keep the first and last units fixed and equalize the gaps between them.</summary>
    public void DistributeSelection(bool horizontal)
    {
        if (!CanDistribute) return;
        var units = ArrangementUnits().Select(es=>(Elements:es, Bounds:ElementGroups.Bounds(es.Select(e=>ElementGeometry.DrawingBounds(e)))))
            .OrderBy(u=>horizontal?u.Bounds.X:u.Bounds.Y).ToList();
        var first=units[0].Bounds;var last=units[^1].Bounds;
        var span=horizontal?last.Right-first.X:last.Bottom-first.Y;
        var gap=(span-units.Sum(u=>horizontal?u.Bounds.Width:u.Bounds.Height))/(units.Count-1);
        var cursor=horizontal?first.X:first.Y;
        Edit("Интервалы выровнены",_=>
        {
            foreach(var unit in units)
            {
                var delta=cursor-(horizontal?unit.Bounds.X:unit.Bounds.Y);
                foreach(var e in unit.Elements){if(horizontal)e.X+=delta;else e.Y+=delta;}
                cursor+=(horizontal?unit.Bounds.Width:unit.Bounds.Height)+gap;
            }
        });
    }
    public void RotateElement(string id,double angle)
    {
        if(CurrentLayer.Find(id) is not { } e || e.Locked || TabPanels.IsContainer(e.Kind))return;
        angle=ElementGeometry.Angle(angle);if(e.Rotation==angle)return;
        Edit("Поворот изменён",_=>e.Rotation=angle);
    }
}
