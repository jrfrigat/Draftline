using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class StoredTransitionsTests
{
    [Fact]
    public void Removing_a_screen_cleans_inactive_variants_sources_and_nested_menu_items()
    {
        var document = DraftDocument.CreateNew("Переходы"); var target = document.AddLayer("B");
        var button = ElementDefaults.Create(WireframeKind.Button);
        button.TargetLayerId = target.Id; button.Transition = TransitionKind.Navigate;
        button.ChoiceItems.Add(new NavItem { Children = [new NavItem { TargetLayerId = target.Id }] });
        document.StartLayer.Elements.Add(button);
        var alternate = new ScreenAlternate(); alternate.Capture(document.StartLayer); document.StartLayer.Alternates.Add(alternate);
        document.Components.Add(new ComponentDefinition { Elements = [button.Clone()] });
        Assert.True(document.RemoveLayer(target.Id));
        foreach (var element in document.StoredElements)
        {
            Assert.Empty(element.TargetLayerId); Assert.Equal(TransitionKind.None, element.Transition);
            Assert.All(NavItems.Rows(element.ChoiceItems), row => Assert.Empty(row.Item.TargetLayerId));
        }
        alternate.Apply(document.StartLayer);
        Assert.Empty(document.StartLayer.Elements[0].TargetLayerId);
    }

    [Fact]
    public void Updating_a_source_updates_instances_in_inactive_variants()
    {
        var session = new DocumentSession(); var button = session.Add(WireframeKind.Button, 20, 20);
        session.CreateComponent("Кнопка"); session.CreateAlternate("Второй");
        var original = session.CurrentLayer.Alternates[0];
        session.CurrentLayer.Alternates[1].Elements[0].Text = "Правка варианта";
        button.Text = "Обновлено"; session.Select(button.Id); session.UpdateComponentFromSelection();
        Assert.All(original.Elements, element => Assert.Equal("Обновлено", element.Text));
        Assert.Equal("Правка варианта", session.CurrentLayer.Alternates[1].Elements[0].Text);
    }
}
