namespace Draftline.App.Model;

public sealed record SearchHit(string LayerId,string LayerName,string ElementId,string Kind,string Excerpt,int PanelIndex=-1);
public static class DocumentSearch
{
    public static IReadOnlyList<SearchHit> Find(DraftDocument document,string? query)
    {
        query=query?.Trim()??"";if(query.Length==0)return Array.Empty<SearchHit>();
        var results=new List<SearchHit>();
        foreach(var layer in document.Layers)
        foreach(var e in layer.Elements.OrderBy(e=>e.ZIndex))
        {
            var fields=new[]{e.DisplayText,e.Value,e.Placeholder,e.SecondaryText,e.FeedbackText,e.ImageName}
                .Concat(e.Options).Concat(e.Columns).Concat(e.Rows.SelectMany(row=>row))
                .Concat(NavItems.Rows(e.ChoiceItems).SelectMany(row=>new[]{row.Item.Label,row.Item.Value}));
            var match=fields.FirstOrDefault(value=>value.Contains(query,StringComparison.OrdinalIgnoreCase));var panel=-1;
            if(match is null){panel=e.TabContents.FindIndex(value=>value.Contains(query,StringComparison.OrdinalIgnoreCase));if(panel>=0)match=e.TabContents[panel];}
            if(match is null)continue;
            var index=match.IndexOf(query,StringComparison.OrdinalIgnoreCase);var start=Math.Max(0,index-35);
            var excerpt=(start>0?"…":"")+match.Substring(start,Math.Min(120,match.Length-start)).Replace('\n',' ')+(match.Length-start>120?"…":"");
            results.Add(new(layer.Id,layer.Name,e.Id,ElementDefaults.NameOf(e.Kind),excerpt,panel));
        }
        return results;
    }
}
