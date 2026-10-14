namespace Draftline.App.Model;

/// <summary>
/// One wireframe: the sheet it is drawn on and the blocks on it.
///
/// This is what a JSON file holds and what the history snapshots, so it is deliberately plain data -
/// no services, no events, no idea that a browser exists. Everything that changes it does so through
/// <c>DocumentSession</c>, which is where the history is kept.
/// </summary>
public sealed class DraftDocument
{
    /// <summary>
    /// The version of the file format. It is written into every file and read before anything else,
    /// because a file from a newer Draftline may mean something else by the same field name.
    ///
    /// 1 - the first shape of the format: a sheet, a list of blocks, a caption on each.
    /// 2 - blocks carry data: a value, a hint and a second line, and a list row may carry a value.
    /// 3 - a table carries columns and rows.
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>A sheet in pixels, as a new wireframe starts.</summary>
    public const double DefaultCanvasWidth = 960;

    /// <summary>A sheet in pixels, as a new wireframe starts.</summary>
    public const double DefaultCanvasHeight = 640;

    /// <summary>Version of the format this document is written in.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Identity of the document; the browser's project list is keyed by it.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    /// <summary>What the person called this wireframe.</summary>
    public string Name { get; set; } = "Без имени";

    /// <summary>Width of the sheet, in pixels.</summary>
    public double CanvasWidth { get; set; } = DefaultCanvasWidth;

    /// <summary>Height of the sheet, in pixels.</summary>
    public double CanvasHeight { get; set; } = DefaultCanvasHeight;

    /// <summary>The grid the canvas snaps to, in pixels; 1 means no grid.</summary>
    public int GridSize { get; set; } = CanvasGeometry.DefaultGridSize;

    /// <summary>The blocks on the sheet.</summary>
    public List<WireframeElement> Elements { get; set; } = new();

    /// <summary>When the wireframe was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When it was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>An empty wireframe with a name.</summary>
    public static DraftDocument CreateNew(string name) =>
        new() { Name = string.IsNullOrWhiteSpace(name) ? "Без имени" : name };

    /// <summary>The blocks, back to front - the order they are drawn and hit-tested in.</summary>
    public IEnumerable<WireframeElement> InDrawOrder() => Elements.OrderBy(element => element.ZIndex);

    /// <summary>The topmost block under a point, or null when the point is on the sheet itself.</summary>
    public WireframeElement? HitTest(double x, double y) =>
        Elements.Where(element => element.Contains(x, y))
            .OrderByDescending(element => element.ZIndex)
            .FirstOrDefault();

    /// <summary>The next free drawing order.</summary>
    public int NextZIndex() => Elements.Count == 0 ? 1 : Elements.Max(element => element.ZIndex) + 1;

    /// <summary>The block with this identity, or null.</summary>
    public WireframeElement? Find(string? id) =>
        string.IsNullOrEmpty(id) ? null : Elements.FirstOrDefault(element => element.Id == id);

    /// <summary>Puts a block in front of every other one.</summary>
    public void BringToFront(WireframeElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.ZIndex = NextZIndex();
    }

    /// <summary>Puts a block behind every other one.</summary>
    public void SendToBack(WireframeElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.ZIndex = Elements.Count == 0 ? 0 : Elements.Min(other => other.ZIndex) - 1;
    }

    /// <summary>Notes that the document was written to just now.</summary>
    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    /// <summary>A deep copy: the blocks are copies of their own, options included.</summary>
    public DraftDocument Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Id = Id,
        Name = Name,
        CanvasWidth = CanvasWidth,
        CanvasHeight = CanvasHeight,
        GridSize = GridSize,
        Elements = Elements.Select(element => element.Clone()).ToList(),
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt
    };

    /// <summary>
    /// Makes a document usable, whoever wrote it: a file typed by hand, a file from an older version,
    /// a document that lost a field on the way. Missing identities are filled in, the sheet is given
    /// a size, every block is put back inside the canvas and the drawing order is rebuilt from
    /// scratch, so nothing downstream has to defend itself against a half-written document.
    /// </summary>
    public void Normalize()
    {
        SchemaVersion = CurrentSchemaVersion;
        Id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("n") : Id;
        Name = string.IsNullOrWhiteSpace(Name) ? "Без имени" : Name.Trim();
        CanvasWidth = CanvasWidth > 0 ? CanvasWidth : DefaultCanvasWidth;
        CanvasHeight = CanvasHeight > 0 ? CanvasHeight : DefaultCanvasHeight;
        GridSize = GridSize > 0 ? GridSize : CanvasGeometry.DefaultGridSize;

        if (CreatedAt == default)
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }

        if (UpdatedAt == default)
        {
            UpdatedAt = CreatedAt;
        }

        Elements ??= new List<WireframeElement>();

        var identities = new HashSet<string>(StringComparer.Ordinal);
        var order = 1;

        foreach (var element in InDrawOrder())
        {
            if (string.IsNullOrWhiteSpace(element.Id) || !identities.Add(element.Id))
            {
                element.Id = Guid.NewGuid().ToString("n");
                identities.Add(element.Id);
            }

            element.Text ??= string.Empty;
            element.Value ??= string.Empty;
            element.Placeholder ??= string.Empty;
            element.SecondaryText ??= string.Empty;
            element.Options ??= new List<string>();
            element.Columns ??= new List<string>();
            element.Rows ??= new List<List<string>>();

            if (element.Columns.Count > 0)
            {
                for (var index = 0; index < element.Rows.Count; index++)
                {
                    var row = element.Rows[index] ??= new List<string>();

                    // A table is read as a rectangle: a shorter row is filled with empty cells and a
                    // longer one is cut. Otherwise every renderer downstream would have to defend
                    // itself against a row that does not match its header.
                    while (row.Count < element.Columns.Count)
                    {
                        row.Add(string.Empty);
                    }

                    while (row.Count > element.Columns.Count)
                    {
                        row.RemoveAt(row.Count - 1);
                    }

                    for (var cell = 0; cell < row.Count; cell++)
                    {
                        row[cell] ??= string.Empty;
                    }
                }
            }
            element.FontScale = element.FontScale > 0 ? element.FontScale : 1;

            var bounds = CanvasGeometry.Clamp(element.Bounds, CanvasWidth, CanvasHeight);
            element.X = bounds.X;
            element.Y = bounds.Y;
            element.Width = bounds.Width;
            element.Height = bounds.Height;

            element.ZIndex = order++;
        }
    }
}
