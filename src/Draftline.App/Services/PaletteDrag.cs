using Draftline.App.Model;

namespace Draftline.App.Services;
/// <summary>
/// The block the palette is being dragged out of it right now.
///
/// A drag from the shelf to the sheet happens inside one app, so what is travelling does not have to
/// be encoded into the browser's drag data at all: the palette says what it picked up, the canvas
/// asks what is being carried when the drop arrives, and the answer is taken away so the next drop
/// cannot reuse it. One shared object instead of a string parsed back out of an event.
/// </summary>
public sealed class PaletteDrag
{
    /// <summary>What is being dragged, or null when nothing is.</summary>
    public WireframeKind? Kind { get; private set; }

    /// <summary>The palette has picked a block up.</summary>
    public void Start(WireframeKind kind) => Kind = kind;
    /// <summary>The drag ended without a drop - the palette says so when the gesture is over.</summary>
    public void End() => Kind = null;
    /// <summary>What was being dragged, and forgets it.</summary>
    public WireframeKind? Take()
    {
        var kind = Kind;
        Kind = null;
        return kind;
    }
}
