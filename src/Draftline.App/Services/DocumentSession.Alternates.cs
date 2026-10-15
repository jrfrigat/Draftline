using Draftline.App.Model;
namespace Draftline.App.Services;
public sealed partial class DocumentSession
{
    public void CreateAlternate(string name)
    {
        var layer=CurrentLayer;
        Edit("Вариант экрана сохранён",_=>
        {
            if(layer.Alternates.Count==0){var original=new ScreenAlternate{Name="Исходный"};original.Capture(layer);layer.Alternates.Add(original);layer.CurrentAlternateId=original.Id;}
            layer.Alternates.FirstOrDefault(a=>a.Id==layer.CurrentAlternateId)?.Capture(layer);
            var alternate=new ScreenAlternate{Name=string.IsNullOrWhiteSpace(name)?"Вариант "+(layer.Alternates.Count+1):name.Trim()};
            alternate.Capture(layer);layer.Alternates.Add(alternate);layer.CurrentAlternateId=alternate.Id;
        });
    }
    public void SwitchAlternate(string id)
    {
        var layer=CurrentLayer;if(layer.CurrentAlternateId==id || layer.Alternates.FirstOrDefault(a=>a.Id==id) is not { } alternate)return;
        Edit("Вариант открыт: "+alternate.Name,_=>{layer.Alternates.FirstOrDefault(a=>a.Id==layer.CurrentAlternateId)?.Capture(layer);alternate.Apply(layer);layer.CurrentAlternateId=id;},false);
        EditingGroupId="";ClearSelection();
    }
    public void SaveCurrentAlternate()
    {
        if(CurrentLayer.Alternates.FirstOrDefault(a=>a.Id==CurrentLayer.CurrentAlternateId) is not { } alternate)return;
        Edit("Вариант обновлён",_=>alternate.Capture(CurrentLayer));
    }
    public void RenameAlternate(string id,string name)
    {
        name=name.Trim();if(name.Length==0 || CurrentLayer.Alternates.FirstOrDefault(a=>a.Id==id) is not { } alternate || alternate.Name==name)return;
        Edit("Вариант переименован",_=>alternate.Name=name);
    }
    public void DeleteAlternate(string id)
    {
        var layer=CurrentLayer;if(layer.Alternates.FirstOrDefault(a=>a.Id==id) is not { } alternate)return;
        Edit("Вариант удалён",_=>
        {
            layer.Alternates.Remove(alternate);
            if(layer.CurrentAlternateId==id)
            {
                if(layer.Alternates.FirstOrDefault() is { } next){next.Apply(layer);layer.CurrentAlternateId=next.Id;}
                else layer.CurrentAlternateId="";
            }
        },false);EditingGroupId="";ClearSelection();
    }
}
