using Draftline.App.Model;

namespace Draftline.App.Services;

/// <summary>Which corner or edge of a block a resize is dragging.</summary>
public enum ResizeHandle
{
    /// <summary>The top-left corner.</summary>
    NorthWest,

    /// <summary>The top edge.</summary>
    North,

    /// <summary>The top-right corner.</summary>
    NorthEast,

    /// <summary>The right edge.</summary>
    East,

    /// <summary>The bottom-right corner.</summary>
    SouthEast,

    /// <summary>The bottom edge.</summary>
    South,

    /// <summary>The bottom-left corner.</summary>
    SouthWest,

    /// <summary>The left edge.</summary>
    West
}

/// <summary>What the pointer is doing on the canvas right now.</summary>
public enum CanvasGesture
{
    /// <summary>Nothing is being dragged.</summary>
    None,

    /// <summary>Blocks are being moved.</summary>
    Move,

    /// <summary>One block is being resized by a handle.</summary>
    Resize,

    /// <summary>A selection frame is being drawn over the sheet.</summary>
    Marquee
}

/// <summary>
/// The rules of dragging things on the canvas, with no browser in sight.
///
/// Every decision the pointer makes - which handle was grabbed, where a block ends up, which blocks a
/// selection frame catches - is arithmetic over rectangles, so it lives here as plain methods. The
/// canvas component then only has to read a coordinate and call one of them, and the behaviour can be
/// reasoned about (and checked) without opening a browser.
/// </summary>
public static class CanvasInteraction
{
    /// <summary>How far from a handle a press still counts as grabbing it, in pixels.</summary>
    public const double HandleRadius = 6;

    /// <summary>The eight handles of a block: where they sit and which edge each one drags.</summary>
    public static IEnumerable<(ResizeHandle Handle, double X, double Y)> HandlesOf(Rect bounds)
    {
        yield return (ResizeHandle.NorthWest, bounds.X, bounds.Y);
        yield return (ResizeHandle.North, bounds.CenterX, bounds.Y);
        yield return (ResizeHandle.NorthEast, bounds.Right, bounds.Y);
        yield return (ResizeHandle.East, bounds.Right, bounds.CenterY);
        yield return (ResizeHandle.SouthEast, bounds.Right, bounds.Bottom);
        yield return (ResizeHandle.South, bounds.CenterX, bounds.Bottom);
        yield return (ResizeHandle.SouthWest, bounds.X, bounds.Bottom);
        yield return (ResizeHandle.West, bounds.X, bounds.CenterY);
    }

    /// <summary>The handle under a point, or null when the point is not on one.</summary>
    public static ResizeHandle? HandleAt(Rect bounds, double x, double y)
    {
        foreach (var (handle, handleX, handleY) in HandlesOf(bounds))
        {
            if (Math.Abs(x - handleX) <= HandleRadius && Math.Abs(y - handleY) <= HandleRadius)
            {
                return handle;
            }
        }

        return null;
    }

    /// <summary>True when the handle drags the left edge, so the block grows to the left.</summary>
    public static bool DragsLeft(ResizeHandle handle) =>
        handle is ResizeHandle.NorthWest or ResizeHandle.West or ResizeHandle.SouthWest;

    /// <summary>True when the handle drags the right edge.</summary>
    public static bool DragsRight(ResizeHandle handle) =>
        handle is ResizeHandle.NorthEast or ResizeHandle.East or ResizeHandle.SouthEast;

    /// <summary>True when the handle drags the top edge.</summary>
    public static bool DragsTop(ResizeHandle handle) =>
        handle is ResizeHandle.NorthWest or ResizeHandle.North or ResizeHandle.NorthEast;

    /// <summary>True when the handle drags the bottom edge.</summary>
    public static bool DragsBottom(ResizeHandle handle) =>
        handle is ResizeHandle.SouthWest or ResizeHandle.South or ResizeHandle.SouthEast;

    /// <summary>
    /// Where a block ends up when a handle is dragged by a delta.
    ///
    /// Always computed from the rectangle the gesture started with, never from the one on screen: a
    /// resize driven from the current rectangle drifts, because every step inherits the rounding of
    /// the step before it. The edge that is not being dragged stays exactly where it was, and neither
    /// the smallest size nor the sheet can be crossed.
    /// </summary>
    public static Rect Resize(
        Rect origin,
        ResizeHandle handle,
        double deltaX,
        double deltaY,
        int gridSize,
        double canvasWidth,
        double canvasHeight)
    {
        var left = origin.X;
        var top = origin.Y;
        var right = origin.Right;
        var bottom = origin.Bottom;

        if (DragsLeft(handle))
        {
            left = CanvasGeometry.Snap(origin.X + deltaX, gridSize);
        }

        if (DragsRight(handle))
        {
            right = CanvasGeometry.Snap(origin.Right + deltaX, gridSize);
        }

        if (DragsTop(handle))
        {
            top = CanvasGeometry.Snap(origin.Y + deltaY, gridSize);
        }

        if (DragsBottom(handle))
        {
            bottom = CanvasGeometry.Snap(origin.Bottom + deltaY, gridSize);
        }

        // A block may not be turned inside out: the edge that would cross the other one stops at the
        // smallest size a person can still grab.
        if (right - left < CanvasGeometry.MinimumSize)
        {
            if (DragsLeft(handle))
            {
                left = right - CanvasGeometry.MinimumSize;
            }
            else
            {
                right = left + CanvasGeometry.MinimumSize;
            }
        }

        if (bottom - top < CanvasGeometry.MinimumSize)
        {
            if (DragsTop(handle))
            {
                top = bottom - CanvasGeometry.MinimumSize;
            }
            else
            {
                bottom = top + CanvasGeometry.MinimumSize;
            }
        }

        left = Math.Max(0, left);
        top = Math.Max(0, top);
        right = Math.Min(canvasWidth, right);
        bottom = Math.Min(canvasHeight, bottom);

        return new Rect(
            left,
            top,
            Math.Max(CanvasGeometry.MinimumSize, right - left),
            Math.Max(CanvasGeometry.MinimumSize, bottom - top));
    }

    /// <summary>The rectangle a frame covers between the point it started at and where the pointer is.</summary>
    public static Rect MarqueeBetween(double startX, double startY, double x, double y) =>
        new(Math.Min(startX, x), Math.Min(startY, y), Math.Abs(x - startX), Math.Abs(y - startY));

    /// <summary>True when two rectangles overlap at all; a frame with no area overlaps nothing.</summary>
    public static bool Intersects(Rect first, Rect second) =>
        first.Width > 0 && first.Height > 0
        && first.X <= second.Right && second.X <= first.Right
        && first.Y <= second.Bottom && second.Y <= first.Bottom;

    /// <summary>The offset an arrow key moves a block by: one grid step, or one pixel when fine.</summary>
    public static double NudgeStep(int gridSize, bool fine) =>
        fine ? 1 : gridSize > 1 ? gridSize : 1;
}
