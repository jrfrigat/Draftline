using System.Text.Json.Serialization;

namespace Draftline.App.Model;

/// <summary>
/// The blocks a wireframe is drawn from.
///
/// The set is small on purpose. A low-fidelity sketch is a sentence about a screen, and a sentence
/// made of twelve words is still a sentence; every kind here exists because a screen cannot be
/// described without it. Nothing in this list is a decoration, and nothing in it is a style.
///
/// A block is written into a file by its name rather than by its number. A number would be shorter
/// and would break every file ever saved the day a kind is inserted in the middle of this list - and
/// a wireframe file is meant to be readable a year from now, by a person as much as by the app.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<WireframeKind>))]
public enum WireframeKind
{
    /// <summary>A region that groups the blocks placed inside it.</summary>
    Panel,

    /// <summary>Body copy.</summary>
    Text,

    /// <summary>A screen or section title.</summary>
    Heading,

    /// <summary>A button.</summary>
    Button,

    /// <summary>A single-line input.</summary>
    TextField,

    /// <summary>A multi-line input.</summary>
    TextArea,

    /// <summary>A checkbox with its label.</summary>
    Checkbox,

    /// <summary>A radio button with its label.</summary>
    Radio,

    /// <summary>A dropdown.</summary>
    Select,

    /// <summary>A list of rows.</summary>
    List,

    /// <summary>An image placeholder.</summary>
    Image,

    /// <summary>A horizontal rule.</summary>
    Divider,

    /// <summary>A table: a header row of columns and the rows underneath them.</summary>
    DataGrid,

    /// <summary>A row of tabs.</summary>
    Tabs,

    /// <summary>A menu bar: the words File, Edit, View across the top of a screen.</summary>
    MenuBar,

    /// <summary>An accordion: a stack of sections that open one at a time.</summary>
    Accordion,

    /// <summary>A tree of nested items.</summary>
    Tree,

    /// <summary>A progress bar.</summary>
    Progress,

    /// <summary>A slider with a handle.</summary>
    Slider,

    /// <summary>A badge or chip: a short label stating a status.</summary>
    Badge,

    /// <summary>A circle carrying initials.</summary>
    Avatar,

    /// <summary>A month grid.</summary>
    Calendar,

    /// <summary>A map with a marker.</summary>
    Map,

    /// <summary>A video placeholder.</summary>
    Video,

    /// <summary>Breadcrumbs: where this screen sits in the hierarchy.</summary>
    Breadcrumbs,

    /// <summary>A status bar along the bottom.</summary>
    StatusBar,

    Link, Icon, Switch, Stepper, Tooltip, Popover, DatePicker, TimePicker, ColorPicker
}
