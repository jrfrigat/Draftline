using Draftline.App.Model;
namespace Draftline.App.Services;
public sealed partial class DocumentSession
{
    /// <summary>Selects and toggles one authored tree node.</summary>
    public void SelectTreeNode(string id, int index)
    {
        if (CurrentLayer.Find(id) is not { Kind: WireframeKind.Tree } tree) return;
        Edit("Узел дерева изменён", _ => TreeState.Choose(tree, index));
        Select(id);
    }
}
