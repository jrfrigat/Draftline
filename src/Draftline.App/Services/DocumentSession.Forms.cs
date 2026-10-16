using Draftline.App.Model;

namespace Draftline.App.Services;
public sealed partial class DocumentSession
{
    /// <summary>Changes a checkbox or radio through one undoable edit.</summary>
    public void SetCheck(string id, bool value, bool mixed = false)
    {
        if (CurrentLayer.Find(id)is not { } element || !ElementDefaults.HasCheck(element.Kind))
            return;
        Edit("Выбор изменён", _ => FormState.Check(CurrentLayer.Elements, element, value, mixed));
    }
}
