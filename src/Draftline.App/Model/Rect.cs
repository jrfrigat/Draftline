namespace Draftline.App.Model;

/// <summary>
/// A block's rectangle on the canvas, in the document's own pixels.
///
/// A value type on purpose: geometry travels through the interaction code by value, so nothing can
/// change a rectangle behind the back of the code that is drawing it.
/// </summary>
/// <param name="X">Distance from the canvas' left edge.</param>
/// <param name="Y">Distance from the canvas' top edge.</param>
/// <param name="Width">Width of the rectangle.</param>
/// <param name="Height">Height of the rectangle.</param>
public readonly record struct Rect(double X, double Y, double Width, double Height)
{
    /// <summary>The right edge.</summary>
    public double Right => X + Width;

    /// <summary>The bottom edge.</summary>
    public double Bottom => Y + Height;

    /// <summary>The horizontal middle.</summary>
    public double CenterX => X + (Width / 2);

    /// <summary>The vertical middle.</summary>
    public double CenterY => Y + (Height / 2);

    /// <summary>True when the point falls inside the rectangle, its edges included.</summary>
    public bool Contains(double x, double y) => x >= X && x <= Right && y >= Y && y <= Bottom;

    /// <summary>The same rectangle with some of its sides replaced.</summary>
    public Rect With(double? x = null, double? y = null, double? width = null, double? height = null) =>
        new(x ?? X, y ?? Y, width ?? Width, height ?? Height);
}
