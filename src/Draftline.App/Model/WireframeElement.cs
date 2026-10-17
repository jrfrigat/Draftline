using System.Text.Json.Serialization;

namespace Draftline.App.Model;
/// <summary>
/// One block of a wireframe: where it sits, how big it is, what it says and how it is drawn.
///
/// Properties rather than a record because both of its users change one field at a time - the
/// inspector edits a number, the canvas edits a coordinate - and because the JSON contract wants a
/// plain object with a stable shape. <see cref = "Clone"/> is what both the history and duplication
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
    /// <summary>Clockwise rotation in degrees around the block's center.</summary>
    public double Rotation { get; set; }
    public string ImageData { get; set; } = "";
    public string ImageName { get; set; } = "";
    public double ImageWidth { get; set; }
    public double ImageHeight { get; set; }
    public double CropX { get; set; }
    public double CropY { get; set; }
    public double CropWidth { get; set; } = 1;
    public double CropHeight { get; set; } = 1;
    public string ImageFit { get; set; } = "contain";
    public bool KeepImageAspect { get; set; } = true;
    public string IconKey { get; set; } = "";
    public bool IconOnly { get; set; }
    public string ComponentKey { get; set; } = "";
    public string InstanceKey { get; set; } = "";
    public string SourceKey { get; set; } = "";
    public string ComponentBaseline { get; set; } = "";
    public double ComponentOriginX { get; set; }
    public double ComponentOriginY { get; set; }
    /// <summary>What the block shows. Empty means the kind's own placeholder.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Rows of a list or a dropdown; every other kind ignores it.</summary>
    public List<string> Options { get; set; } = new();
    /// <summary>Metadata and submenus aligned with Options.</summary>
    public List<NavItem> ChoiceItems { get; set; } = new();
    /// <summary>Selected item path within a submenu.</summary>
    public string SelectedPath { get; set; } = string.Empty;
    /// <summary>Visible rows before scrolling; zero shows all rows.</summary>
    public int MaxVisibleItems { get; set; }
    /// <summary>
    /// What stands in the block, as opposed to what it is called: the text someone typed into a field,
    /// the row a dropdown is showing. A block that says "Логин" is a label; one that says
    /// "Логин: ivan@example.com" is a screen somebody has already used, and that is the difference
    /// between a drawing of a form and a description of one.
    /// </summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>The hint an empty field carries - what the input says before anyone typed in it.</summary>
    public string Placeholder { get; set; } = string.Empty;
    /// <summary>The second line of a block: a row's detail, a panel's description, a caption.</summary>
    public string SecondaryText { get; set; } = string.Empty;
    /// <summary>Unit displayed beside a metric.</summary>
    public string StatUnit { get; set; } = string.Empty;
    /// <summary>Formatted change in a metric.</summary>
    public string StatDelta { get; set; } = string.Empty;
    /// <summary>Semantic change colour: default, success, warning, error or info.</summary>
    public string StatTone { get; set; } = "default";
    /// <summary>Section label above a page heading.</summary>
    public string HeaderEyebrow { get; set; } = string.Empty;
    /// <summary>Label of the page header's primary action; empty hides it.</summary>
    public string HeaderAction { get; set; } = string.Empty;
    /// <summary>Semantic heading level, from 1 to 6.</summary>
    public int HeadingLevel { get; set; } = 1;
    /// <summary>Column headers of a table; every other kind ignores them.</summary>
    public List<string> Columns { get; set; } = new();
    /// <summary>
    /// The rows of a table, each row holding its cells. A row that is shorter than the header is padded
    /// and a longer one is cut when the document is read, so a table is always a rectangle by the time
    /// anything draws it.
    /// </summary>
    public List<List<string>> Rows { get; set; } = new();
    /// <summary>Table appearance and row/sort state; indices refer to authored data.</summary>
    public TableStyle TableStyle { get; set; }
    public int SelectedRow { get; set; } = -1;
    public int SortColumn { get; set; } = -1;
    public bool SortDescending { get; set; }
    /// <summary>Slider range and shared vertical orientation.</summary>
    public double Minimum { get; set; }
    public double Maximum { get; set; } = 100;
    public double Step { get; set; } = 1;
    public bool Vertical { get; set; }
    /// <summary>ISO displayed month and selected calendar date.</summary>
    public string CalendarMonth { get; set; } = string.Empty;
    public string SelectedDate { get; set; } = string.Empty;
    /// <summary>What the block does when it is pressed in the preview: nothing, navigate, open a dialog.</summary>
    public TransitionKind Transition { get; set; } = TransitionKind.None;
    /// <summary>The screen a transition leads to; empty when the block leads nowhere.</summary>
    public string TargetLayerId { get; set; } = string.Empty;
    /// <summary>True when pressing this block in the preview goes somewhere.</summary>
    public bool HasTransition => Transition != TransitionKind.None && !string.IsNullOrEmpty(TargetLayerId);
    /// <summary>Whether a checkbox or a radio button is ticked.</summary>
    public bool Checked { get; set; }
    /// <summary>Mixed checkbox state; also used for an indeterminate progress indicator.</summary>
    public bool Indeterminate { get; set; }
    /// <summary>Radio controls with the same nonempty group on a screen exclude each other.</summary>
    public string RadioGroup { get; set; } = string.Empty;
    /// <summary>Authored visual state of a form control.</summary>
    public ControlVisualState VisualState { get; set; }
    /// <summary>Supporting or error text of an input.</summary>
    public string FeedbackText { get; set; } = string.Empty;
    /// <summary>Selected row, tab or section; -1 means no explicit selection.</summary>
    public int SelectedIndex { get; set; } = -1;
    /// <summary>Text in each tab panel, indexed like Options; removed panels retain their text.</summary>
    public List<string> TabContents { get; set; } = new();
    /// <summary>Depth of each tree row, aligned with Options.</summary>
    public List<int> TreeDepths { get; set; } = new();
    /// <summary>Indices of expanded tree branches.</summary>
    public List<int> ExpandedNodes { get; set; } = new();
    /// <summary>Nested group identities, outermost first.</summary>
    public List<string> GroupPath { get; set; } = new();
    /// <summary>Identity of the tab group this block belongs to, or empty for a sheet-level block.</summary>
    public string ParentTabsId { get; set; } = string.Empty;
    /// <summary>The panel within ParentTabsId. Coordinates remain relative to the sheet.</summary>
    public int ParentTabIndex { get; set; }
    /// <summary>Authored disclosure state; null uses the kind's default (trees open, other kinds closed).</summary>
    public bool? Expanded { get; set; }
    /// <summary>Whether the control is shown as unavailable and ignores preview interactions.</summary>
    public bool Disabled { get; set; }
    /// <summary>Protects the block's position and size while leaving its content editable.</summary>
    public bool Locked { get; set; }
    /// <summary>Excluded from drawing, preview, hit testing and export.</summary>
    public bool Hidden { get; set; }
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
    /// <summary>What stands in the block: its value, or the hint when nothing stands there yet.</summary>
    public string DisplayValue => string.IsNullOrWhiteSpace(Value) ? Placeholder : Value;
    /// <summary>
    /// True when the block has no value and is only showing its hint. The canvas draws a hint in a
    /// lighter ink, because "nothing typed here" and "typed nothing" are two different screens.
    /// </summary>
    public bool ShowsHint => string.IsNullOrWhiteSpace(Value);
    /// <summary>The rows of a list or a dropdown, each split into its label and its value.</summary>
    [JsonIgnore]
    public IEnumerable<(string Label, string Value)> OptionRows => Options.Select(ElementDefaults.SplitOption);

    /// <summary>True when the point falls inside the block.</summary>
    public bool Contains(double x, double y)
    {
        var point = ElementGeometry.Rotate(x, y, Bounds, -Rotation);
        return Bounds.Contains(point.X, point.Y);
    }

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
        Rotation = Rotation,
        ImageData = ImageData,
        ImageName = ImageName,
        ImageWidth = ImageWidth,
        ImageHeight = ImageHeight,
        IconKey = IconKey,
        IconOnly = IconOnly,
        ComponentKey = ComponentKey,
        InstanceKey = InstanceKey,
        SourceKey = SourceKey,
        ComponentBaseline = ComponentBaseline,
        ComponentOriginX = ComponentOriginX,
        ComponentOriginY = ComponentOriginY,
        CropX = CropX,
        CropY = CropY,
        CropWidth = CropWidth,
        CropHeight = CropHeight,
        ImageFit = ImageFit,
        KeepImageAspect = KeepImageAspect,
        Text = Text,
        Options = new List<string>(Options),
        ChoiceItems = ChoiceItems.Select(item => item.Clone()).ToList(),
        SelectedPath = SelectedPath,
        MaxVisibleItems = MaxVisibleItems,
        Value = Value,
        Placeholder = Placeholder,
        SecondaryText = SecondaryText,
        StatUnit = StatUnit,
        StatDelta = StatDelta,
        StatTone = StatTone,
        HeaderEyebrow = HeaderEyebrow,
        HeaderAction = HeaderAction,
        HeadingLevel = HeadingLevel,
        Columns = new List<string>(Columns),
        Rows = Rows.Select(row => new List<string>(row)).ToList(),
        TableStyle = TableStyle,
        SelectedRow = SelectedRow,
        SortColumn = SortColumn,
        SortDescending = SortDescending,
        Minimum = Minimum,
        Maximum = Maximum,
        Step = Step,
        Vertical = Vertical,
        CalendarMonth = CalendarMonth,
        SelectedDate = SelectedDate,
        Transition = Transition,
        TargetLayerId = TargetLayerId,
        Checked = Checked,
        Indeterminate = Indeterminate,
        RadioGroup = RadioGroup,
        VisualState = VisualState,
        FeedbackText = FeedbackText,
        SelectedIndex = SelectedIndex,
        TabContents = new List<string>(TabContents),
        TreeDepths = new List<int>(TreeDepths),
        ExpandedNodes = new List<int>(ExpandedNodes),
        GroupPath = new List<string>(GroupPath),
        ParentTabsId = ParentTabsId,
        ParentTabIndex = ParentTabIndex,
        Expanded = Expanded,
        Disabled = Disabled,
        Locked = Locked,
        Hidden = Hidden,
        FontScale = FontScale,
        Bold = Bold,
        ZIndex = ZIndex
    };
}
