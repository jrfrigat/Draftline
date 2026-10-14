namespace Draftline.App.Model;

/// <summary>
/// One block of a wireframe: where it sits, how big it is, what it says and how it is drawn.
///
/// Properties rather than a record because both of its users change one field at a time - the
/// inspector edits a number, the canvas edits a coordinate - and because the JSON contract wants a
/// plain object with a stable shape. <see cref="Clone"/> is what both the history and duplication
/// use, so a block never shares a list with its copy.
/// </summary>
public sealed class WireframeElement
{
    /// <summary>Stable identity of the block inside its document. Never reused.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Which block this is.</summary>
    public WireframeKind Kind { get; set; }

    /// <summary>Distance from the canvas' left edge, in pixels.</summary>
    public double X { get; set; }

    /// <summary>Distance from the canvas' top edge, in pixels.</summary>
    public double Y { get; set; }

    /// <summary>Width in pixels.</summary>
    public double Width { get; set; }

    /// <summary>Height in pixels.</summary>
    public double Height { get; set; }

    /// <summary>What the block shows. Empty means the kind's own placeholder.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Rows of a list or a dropdown; every other kind ignores it.</summary>
    public List<string> Options { get; set; } = new();

    /// <summary>Whether a checkbox or a radio button is ticked.</summary>
    public bool Checked { get; set; }

    /// <summary>Text scale, 1 being the kind's own size.</summary>
    public double FontScale { get; set; } = 1;

    /// <summary>Whether the block's text is bold - the one emphasis a wireframe gets to make.</summary>
    public bool Bold { get; set; }

    /// <summary>Drawing order: the larger the number, the closer to the reader.</summary>
    public int ZIndex { get; set; }

    /// <summary>The block's rectangle on the canvas.</summary>
    public Rect Bounds => new(X, Y, Width, Height);

    /// <summary>The words to draw: the block's own, or the placeholder its kind carries.</summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Text) ? ElementDefaults.PlaceholderFor(Kind) : Text;

    /// <summary>True when the point falls inside the block.</summary>
    public bool Contains(double x, double y) => Bounds.Contains(x, y);

    /// <summary>
    /// A copy of the block. The identity is kept by default, because the history snapshots a document
    /// as it is; duplication asks for a new identity, because a copy is a block of its own.
    /// </summary>
    public WireframeElement Clone(bool keepId = true) => new()
    {
        Id = keepId ? Id : Guid.NewGuid().ToString("n"),
        Kind = Kind,
        X = X,
        Y = Y,
        Width = Width,
        Height = Height,
        Text = Text,
        Options = new List<string>(Options),
        Checked = Checked,
        FontScale = FontScale,
        Bold = Bold,
        ZIndex = ZIndex
    };
}
