using System.Text.Json.Serialization;

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
    /// 4 - a wireframe carries screens, and a block may point at another one.
    /// </summary>
    /// 5 - authored component states. 6 - tab panel content and block membership.
    // 7: per-component data. 8: extended palette, canvas organization and reusable assets.
    public const int CurrentSchemaVersion = 8;

    /// <summary>A sheet in pixels, as a new screen starts.</summary>
    public const double DefaultCanvasWidth = 960;

    /// <summary>A sheet in pixels, as a new screen starts.</summary>
    public const double DefaultCanvasHeight = 640;

    /// <summary>Version of the format this document is written in.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>Identity of the document; the browser's project list is keyed by it.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("n");

    /// <summary>What the person called this wireframe.</summary>
    public string Name { get; set; } = "Без имени";

    /// <summary>The grid the canvas snaps to, in pixels; 1 means no grid.</summary>
    public int GridSize { get; set; } = CanvasGeometry.DefaultGridSize;

    /// <summary>The screens of this wireframe, in the order they are listed.</summary>
    public List<DraftLayer> Layers { get; set; } = new();
    public List<ComponentDefinition> Components { get; set; } = new();

    /// <summary>The screen a preview opens on; empty means the first one.</summary>
    public string StartLayerId { get; set; } = string.Empty;

    /// <summary>When the wireframe was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When it was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The sheet of a file written before screens existed. It is read, migrated into the first screen by
    /// <see cref="Normalize"/>, and never written again: a current file carries screens only.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double CanvasWidth { get; set; }

    /// <summary>Height of the sheet of a file written before screens existed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double CanvasHeight { get; set; }

    /// <summary>The blocks of a file written before screens existed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<WireframeElement> Elements { get; set; } = new();

    /// <summary>An empty wireframe with a name and one screen to draw on.</summary>
    public static DraftDocument CreateNew(string name)
    {
        var document = new DraftDocument { Name = string.IsNullOrWhiteSpace(name) ? "Без имени" : name };
        var layer = DraftLayer.Create("Экран 1");

        document.Layers.Add(layer);
        document.StartLayerId = layer.Id;

        return document;
    }

    /// <summary>The screen with this identity, or null.</summary>
    public DraftLayer? FindLayer(string? id) =>
        string.IsNullOrEmpty(id) ? null : Layers.FirstOrDefault(layer => layer.Id == id);

    /// <summary>The screen a preview opens on: the one named as the start, or the first there is.</summary>
    public DraftLayer StartLayer => FindLayer(StartLayerId) ?? Layers[0];

    /// <summary>Marks a screen as the one a preview opens on.</summary>
    public void SetStartLayer(string id)
    {
        if (FindLayer(id) is not null)
        {
            StartLayerId = id;
        }
    }

    /// <summary>Adds a screen at the end and returns it.</summary>
    public DraftLayer AddLayer(string name)
    {
        var layer = DraftLayer.Create(name);
        Layers.Add(layer);
        return layer;
    }

    /// <summary>
    /// Removes a screen. The last one is never removed - a wireframe with no screen is not a wireframe -
    /// and the transitions that led to it go with it, because a block pointing at a screen that no
    /// longer exists is a block that would walk the preview into a wall.
    /// </summary>
    public bool RemoveLayer(string id)
    {
        if (Layers.Count <= 1 || FindLayer(id) is not { } layer)
        {
            return false;
        }

        Layers.Remove(layer);

        foreach (var element in AllElements)
        {
            if(element.ComponentKey.Length>0 && (!Components.Any(c=>c.Id==element.ComponentKey) || element.InstanceKey.Length==0))ComponentDefinition.Detach(element);
            if (element.TargetLayerId == id)
            {
                element.Transition = TransitionKind.None;
                element.TargetLayerId = string.Empty;
            }
        }

        if (StartLayerId == id)
        {
            StartLayerId = Layers[0].Id;
        }

        return true;
    }

    /// <summary>Every block of every screen - what a count, an export or a migration walks.</summary>
    public IEnumerable<WireframeElement> AllElements => Layers.SelectMany(layer => layer.Elements);

    /// <summary>How many blocks the whole wireframe holds.</summary>
    public int TotalElements => Layers.Sum(layer => layer.Elements.Count);

    /// <summary>Notes that the document was written to just now.</summary>
    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    /// <summary>A deep copy: every screen and every block in it is a copy of its own.</summary>
    public DraftDocument Clone() => new()
    {
        SchemaVersion = SchemaVersion,
        Id = Id,
        Name = Name,
        GridSize = GridSize,
        Layers = Layers.Select(layer => layer.Clone()).ToList(),
        Components = Components.Select(component => component.Clone()).ToList(),
        StartLayerId = StartLayerId,
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
        var legacyTabHeaders = SchemaVersion < 6;
        SchemaVersion = CurrentSchemaVersion;
        Id = string.IsNullOrWhiteSpace(Id) ? Guid.NewGuid().ToString("n") : Id;
        Name = string.IsNullOrWhiteSpace(Name) ? "Без имени" : Name.Trim();
        GridSize = GridSize > 0 ? GridSize : CanvasGeometry.DefaultGridSize;

        if (CreatedAt == default)
        {
            CreatedAt = DateTimeOffset.UtcNow;
        }

        if (UpdatedAt == default)
        {
            UpdatedAt = CreatedAt;
        }

        Layers ??= new List<DraftLayer>();
        Components ??= new();
        var componentIds = new HashSet<string>();
        Components.RemoveAll(c=>c is null);
        foreach(var component in Components)
        {
            if(string.IsNullOrWhiteSpace(component.Id) || !componentIds.Add(component.Id))component.Id=Guid.NewGuid().ToString("n");
            component.Name=string.IsNullOrWhiteSpace(component.Name)?"Компонент":component.Name.Trim();
            var template=DraftLayer.Create("Component");template.CanvasWidth=100000;template.CanvasHeight=100000;template.Elements=component.Elements??new();template.Normalize();
            component.Elements=template.Elements;foreach(var e in component.Elements)ComponentDefinition.Detach(e);
        }

        // A file written before screens existed kept its blocks on the document itself. Those blocks
        // become the one screen such a file has: nothing is invented and nothing is lost, which is the
        // only honest way to read an older picture.
        if (Layers.Count == 0)
        {
            var migrated = DraftLayer.Create("Экран 1");
            migrated.CanvasWidth = CanvasWidth > 0 ? CanvasWidth : DefaultCanvasWidth;
            migrated.CanvasHeight = CanvasHeight > 0 ? CanvasHeight : DefaultCanvasHeight;
            migrated.Elements = Elements ?? new List<WireframeElement>();
            Layers.Add(migrated);
        }

        var identities = new HashSet<string>(StringComparer.Ordinal);

        foreach (var layer in Layers)
        {
            // The size an older file named belongs to the screen it had, so a screen that arrives
            // without one takes it before the default does.
            if (layer.CanvasWidth <= 0)
            {
                layer.CanvasWidth = CanvasWidth > 0 ? CanvasWidth : DefaultCanvasWidth;
            }

            if (layer.CanvasHeight <= 0)
            {
                layer.CanvasHeight = CanvasHeight > 0 ? CanvasHeight : DefaultCanvasHeight;
            }

            if (legacyTabHeaders)
            {
                foreach (var tabs in (layer.Elements ?? new List<WireframeElement>()).Where(element => element.Kind == WireframeKind.Tabs &&
                             element.Height <= TabPanels.HeaderHeight))
                    tabs.Height = ElementDefaults.SizeOf(WireframeKind.Tabs).Height;
            }
            layer.Normalize();

            if (!identities.Add(layer.Id))
            {
                layer.Id = Guid.NewGuid().ToString("n");
                identities.Add(layer.Id);
            }
        }

        if (FindLayer(StartLayerId) is null)
        {
            StartLayerId = Layers[0].Id;
        }

        // A transition into a screen that no longer exists is not a transition: it is cleared here
        // rather than left pointing at nothing, because a preview would otherwise walk into a wall.
        foreach (var element in AllElements)
        {
            if (element.Transition != TransitionKind.None && FindLayer(element.TargetLayerId) is null)
            {
                element.Transition = TransitionKind.None;
                element.TargetLayerId = string.Empty;
            }
        }

        // The members an older file used are emptied, so writing this document back carries screens only.
        CanvasWidth = 0;
        CanvasHeight = 0;
        Elements = new List<WireframeElement>();
    }
}
