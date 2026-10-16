using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class GestureHistoryTests
{
    [Fact]
    public void Undo_during_drag_is_ignored_and_the_gesture_remains_one_history_step()
    {
        var session = new DocumentSession(); var element = session.Add(WireframeKind.Button, 40, 40);
        session.BeginGesture("Движение"); session.Preview(_ => element.X = 80);
        Assert.False(session.Undo()); Assert.False(session.Redo());
        Assert.True(session.EndGesture()); Assert.True(session.Undo());
        Assert.Equal(40, session.CurrentLayer.Find(element.Id)!.X);
        Assert.True(session.Redo()); Assert.Equal(80, session.CurrentLayer.Find(element.Id)!.X);
    }

    [Fact]
    public void A_command_finishes_the_gesture_and_stale_pointer_events_do_not_change_the_document()
    {
        var session = new DocumentSession(); var element = session.Add(WireframeKind.Button, 40, 40);
        session.BeginGesture("Движение"); session.Preview(_ => element.X = 80);
        session.RemoveSelected();
        session.Preview(_ => throw new InvalidOperationException("stale callback"));
        Assert.False(session.IsGestureRunning); Assert.Empty(session.CurrentLayer.Elements);
        Assert.True(session.Undo()); Assert.Equal(80, session.CurrentLayer.Find(element.Id)!.X);
        Assert.True(session.Undo()); Assert.Equal(40, session.CurrentLayer.Find(element.Id)!.X);
    }
}
