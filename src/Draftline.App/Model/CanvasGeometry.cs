namespace Draftline.App.Model;

/// <summary>Which way a block is moved when it is aligned on the sheet.</summary>
public enum ElementAlignment
{
    /// <summary>To the left edge of the sheet.</summary>
    Left,

    /// <summary>To the horizontal middle of the sheet.</summary>
    CenterHorizontally,

    /// <summary>To the right edge of the sheet.</summary>
    Right,

    /// <summary>To the top edge of the sheet.</summary>
    Top,

    /// <summary>To the vertical middle of the sheet.</summary>
    CenterVertically,

    /// <summary>To the bottom edge of the sheet.</summary>
    Bottom,

    /// <summary>As wide as the sheet.</summary>
    MatchWidth,

    /// <summary>As tall as the sheet.</summary>
    MatchHeight
}

/// <summary>
/// The arithmetic of putting a block on a sheet: snapping it to the grid, keeping it at a size a
/// person can still grab, and stopping it at the canvas edge.
///
/// It lives on its own, apart from any component, because these are rules rather than rendering: a
/// block that escapes its canvas is a mistake of geometry, and there is exactly one place to fix it.
/// </summary>
public static class CanvasGeometry
{
    /// <summary>The grid the canvas snaps to, in pixels; 1 turns snapping off.</summary>
    public const int DefaultGridSize = 8;

    /// <summary>The smallest a block may be dragged down to.</summary>
    public const double MinimumSize = 8;

    /// <summary>
    /// Rounds a coordinate to the grid. A grid of 1 (or less) rounds to whole pixels instead.
    /// Rounding goes away from zero: half a step lands on the next grid line, which is what a hand
    /// dragging a block expects, and it keeps the rule out of the reach of .NET's banker's rounding.
    /// </summary>
    public static double Snap(double value, int gridSize) =>
        gridSize <= 1
            ? Math.Round(value, MidpointRounding.AwayFromZero)
            : Math.Round(value / gridSize, MidpointRounding.AwayFromZero) * gridSize;

    /// <summary>
    /// Keeps a rectangle inside the canvas: a block may be moved to an edge but not past it, and a
    /// block larger than the canvas is pinned to its top-left corner rather than pushed around.
    /// </summary>
    public static Rect Clamp(Rect rect, double canvasWidth, double canvasHeight)
    {
        var width = Math.Max(MinimumSize, Math.Min(rect.Width, canvasWidth));
        var height = Math.Max(MinimumSize, Math.Min(rect.Height, canvasHeight));
        var x = Math.Clamp(rect.X, 0, Math.Max(0, canvasWidth - width));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, canvasHeight - height));
        return new Rect(x, y, width, height);
    }

    /// <summary>Moves a rectangle by a delta and keeps the result on the sheet.</summary>
    public static Rect Move(Rect rect, double deltaX, double deltaY, double canvasWidth, double canvasHeight) =>
        Clamp(rect.With(x: rect.X + deltaX, y: rect.Y + deltaY), canvasWidth, canvasHeight);

    /// <summary>The rectangle of a block.</summary>
    public static Rect BoundsOf(WireframeElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.Bounds;
    }

    /// <summary>
    /// Where a block sits when it is aligned on the sheet. The result is snapped to the grid and
    /// clamped to the sheet, so an alignment can never put a block where a hand could not.
    /// </summary>
    public static Rect Align(Rect rect, ElementAlignment alignment, int gridSize, double canvasWidth, double canvasHeight)
    {
        var aligned = alignment switch
        {
            ElementAlignment.Left => rect.With(x: 0),
            ElementAlignment.CenterHorizontally => rect.With(x: (canvasWidth - rect.Width) / 2),
            ElementAlignment.Right => rect.With(x: canvasWidth - rect.Width),
            ElementAlignment.Top => rect.With(y: 0),
            ElementAlignment.CenterVertically => rect.With(y: (canvasHeight - rect.Height) / 2),
            ElementAlignment.Bottom => rect.With(y: canvasHeight - rect.Height),
            ElementAlignment.MatchWidth => rect.With(width: canvasWidth),
            ElementAlignment.MatchHeight => rect.With(height: canvasHeight),
            _ => rect
        };

        return Clamp(
            new Rect(
                Snap(aligned.X, gridSize),
                Snap(aligned.Y, gridSize),
                Snap(aligned.Width, gridSize),
                Snap(aligned.Height, gridSize)),
            canvasWidth,
            canvasHeight);
    }
}
