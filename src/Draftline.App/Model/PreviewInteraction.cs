namespace Draftline.App.Model;

/// <summary>Controls whose state has its own gestures and must never be covered by a navigation hotspot.</summary>
public static class PreviewInteraction
{
    public static bool Stateful(WireframeKind kind)=>kind is
        WireframeKind.TextField or WireframeKind.TextArea or WireframeKind.Checkbox or WireframeKind.Radio or
        WireframeKind.Select or WireframeKind.List or WireframeKind.Tabs or WireframeKind.Accordion or WireframeKind.Tree or
        WireframeKind.MenuBar or WireframeKind.Breadcrumbs or WireframeKind.StatusBar or WireframeKind.DataGrid or
        WireframeKind.Slider or WireframeKind.Calendar or WireframeKind.Switch or WireframeKind.Stepper or
        WireframeKind.Tooltip or WireframeKind.Popover or WireframeKind.DatePicker or WireframeKind.TimePicker or
        WireframeKind.ColorPicker or WireframeKind.AudioPlayer or WireframeKind.RichTextEditor or WireframeKind.Alert or WireframeKind.CoverFlow;
}
