using Draftline.App.Model;
namespace Draftline.App.Services;
public sealed partial class DocumentSession
{
    public bool SearchOpen { get; private set; }
    public int SearchFocusVersion { get; private set; }
    public int SearchNavigationVersion { get; private set; }
    public void ShowSearch(){SearchOpen=true;SearchFocusVersion++;Changed?.Invoke();}
    public void CloseSearch(){if(!SearchOpen)return;SearchOpen=false;Changed?.Invoke();}
    public void SelectSearchResult(SearchHit hit)
    {
        if(Document.FindLayer(hit.LayerId)?.Find(hit.ElementId) is not { } e)return;
        if(IsPreviewing)ExitPreview();SelectLayer(hit.LayerId);
        if(hit.PanelIndex>=0 && TabPanels.IsContainer(e.Kind) && (TabPanels.Index(e)!=hit.PanelIndex || e.Kind==WireframeKind.Accordion && !ElementState.IsExpanded(e)))
            Edit("Найденный раздел показан",_=>{e.SelectedIndex=hit.PanelIndex;e.Expanded=true;});
        RevealElement(hit.ElementId);SearchNavigationVersion++;Changed?.Invoke();
    }
}
