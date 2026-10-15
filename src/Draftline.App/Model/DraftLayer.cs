namespace Draftline.App.Model;

/// <summary>
/// One screen of a wireframe.
///
/// A wireframe of anything real is several screens - a list, the form it opens, the dialog that form
/// raises - and each of them is a sheet with its own blocks on it. Keeping them as layers of one
/// document rather than as separate files is what lets a block point at another screen and say "this
/// is where it goes", which is the part of a wireframe a person cannot write down in one picture.
/// </summary>
public sealed class DraftLayer
{
    /// <summary>Identity of the screen; transitions name it.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    /// <summary>What the person calls this screen.</summary>
    public string Name { get; set; } = "Экран";

    /// <summary>Width of this screen's sheet, in pixels.</summary>
    public double CanvasWidth { get; set; } = DraftDocument.DefaultCanvasWidth;

    /// <summary>Height of this screen's sheet, in pixels.</summary>
    public double CanvasHeight { get; set; } = DraftDocument.DefaultCanvasHeight;

    /// <summary>The blocks on this screen.</summary>
    public List<WireframeElement> Elements { get; set; } = new();

    /// <summary>A screen with a name.</summary>
    public static DraftLayer Create(string name) =>
        new() { Name = string.IsNullOrWhiteSpace(name) ? "Экран" : name.Trim() };

    /// <summary>The blocks, back to front - the order they are drawn and hit-tested in.</summary>
    public IEnumerable<WireframeElement> InDrawOrder() => Elements.OrderBy(element => element.ZIndex);

    /// <summary>The topmost block under a point, or null when the point is on the sheet itself.</summary>
    public WireframeElement? HitTest(double x, double y) =>
        TabPanels.Visible(this).Reverse().Where(element => element.Contains(x, y))
            .FirstOrDefault();

    /// <summary>The block with this identity, or null.</summary>
    public WireframeElement? Find(string? id) =>
        string.IsNullOrEmpty(id) ? null : Elements.FirstOrDefault(element => element.Id == id);

    /// <summary>The next free drawing order.</summary>
    public int NextZIndex() => Elements.Count == 0 ? 1 : Elements.Max(element => element.ZIndex) + 1;

    /// <summary>Puts a block in front of every other one on this screen.</summary>
    public void BringToFront(WireframeElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.ZIndex = NextZIndex();
    }

    /// <summary>Puts a block behind every other one on this screen.</summary>
    public void SendToBack(WireframeElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.ZIndex = Elements.Count == 0 ? 0 : Elements.Min(other => other.ZIndex) - 1;
    }

    /// <summary>A copy of the screen, blocks and all.</summary>
    public DraftLayer Clone() => new()
    {
        Id = Id,
        Name = Name,
        CanvasWidth = CanvasWidth,
        CanvasHeight = CanvasHeight,
        Elements = Elements.Select(element => element.Clone()).ToList()
    };

    /// <summary>
    /// Makes a screen usable, whoever wrote it: a name, a size, and every block put back inside the
    /// sheet with an identity and a drawing order. A row of a table is also brought back to a
    /// rectangle here, because a renderer should never have to defend itself against a half-written
    /// file.
    /// </summary>
    public void Normalize()
    {
        Id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("n") : Id;
        Name = string.IsNullOrWhiteSpace(Name) ? "Экран" : Name.Trim();
        CanvasWidth = CanvasWidth > 0 ? CanvasWidth : DraftDocument.DefaultCanvasWidth;
        CanvasHeight = CanvasHeight > 0 ? CanvasHeight : DraftDocument.DefaultCanvasHeight;
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
            element.TargetLayerId ??= string.Empty;
            element.Options ??= new List<string>();
            ElementState.Normalize(element);
            TreeState.Normalize(element);
            NavItems.Normalize(element);
            element.Columns ??= new List<string>();
            element.Rows ??= new List<List<string>>();
            element.FontScale = element.FontScale > 0 ? element.FontScale : 1;

            if (element.Columns.Count > 0)
            {
                NormalizeRows(element);
            }

            TableState.Normalize(element);
            ControlRange.Normalize(element);
            ExtraElements.Normalize(element);
            CalendarState.Normalize(element);
            var bounds = CanvasGeometry.Clamp(element.Bounds, CanvasWidth, CanvasHeight);
            element.X = bounds.X;
            element.Y = bounds.Y;
            element.Width = bounds.Width;
            element.Height = bounds.Height;

            element.ZIndex = order++;
        }
        TabPanels.Normalize(this);
        FormState.Normalize(this);
    }

    private static void NormalizeRows(WireframeElement element)
    {
        for (var index = 0; index < element.Rows.Count; index++)
        {
            var row = element.Rows[index] ??= new List<string>();

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
}
