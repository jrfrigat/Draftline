using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class CanvasPerformanceTests
{
    [Fact]
    public void Pointer_updates_repaint_the_canvas_and_commit_notifies_other_panels_once()
    {
        var session = new DocumentSession(); var element = session.Add(WireframeKind.Button, 40, 40);
        session.BeginGesture("Drag"); var changes = 0; var frames = 0;
        session.Changed += () => changes++; session.GestureChanged += () => frames++;
        for (var index = 0; index < 500; index++) session.Preview(_ => element.X = index);
        Assert.Equal(0, changes); Assert.Equal(500, frames);
        session.EndGesture(); Assert.Equal(1, changes);
        session.Undo(); Assert.Equal(40, session.CurrentLayer.Find(element.Id)!.X);
    }

    [Fact]
    public void Fragment_cache_reuses_movement_and_invalidates_changed_content()
    {
        var cache = new ElementFragmentCache(); var element = ElementDefaults.Create(WireframeKind.Switch);
        var first = cache.Render(element, null);
        element.X += 100; element.Y += 100;
        Assert.Same(first, cache.Render(element, null));
        element.Checked = true;
        Assert.NotEqual(first, cache.Render(element, null));
        first = cache.Render(element, null); element.Text = "Новая подпись";
        Assert.NotEqual(first, cache.Render(element, null));
    }
}
