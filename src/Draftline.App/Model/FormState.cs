namespace Draftline.App.Model;

/// <summary>Authored appearances available to existing form controls.</summary>
public enum ControlVisualState { Normal, Focused, Selected, Error }

/// <summary>Form state rules shared by authored documents and temporary preview copies.</summary>
public static class FormState
{
    /// <summary>Whether the control supports an authored visual state.</summary>
    public static bool HasVisualState(WireframeKind kind) => kind is WireframeKind.Button or
        WireframeKind.TextField or WireframeKind.TextArea or WireframeKind.Select;

    /// <summary>Whether helper/error feedback belongs to the control.</summary>
    public static bool HasFeedback(WireframeKind kind) => kind is WireframeKind.TextField or WireframeKind.TextArea or WireframeKind.Select;

    /// <summary>Sets a check state and enforces exclusivity only within a named radio group.</summary>
    public static void Check(IEnumerable<WireframeElement> elements, WireframeElement element, bool value, bool mixed = false)
    {
        element.Indeterminate = mixed && element.Kind == WireframeKind.Checkbox;
        element.Checked = value && !element.Indeterminate;
        if (element.Kind == WireframeKind.Radio && value && !string.IsNullOrEmpty(element.RadioGroup))
            foreach (var other in elements.Where(other => other.Id != element.Id && other.Kind == WireframeKind.Radio &&
                         other.RadioGroup == element.RadioGroup)) other.Checked = false;
    }

    /// <summary>Sanitizes imported states and makes malformed radio groups deterministic.</summary>
    public static void Normalize(DraftLayer layer)
    {
        var checkedGroups = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in layer.InDrawOrder())
        {
            element.RadioGroup = element.RadioGroup?.Trim() ?? string.Empty;
            element.FeedbackText ??= string.Empty;
            if (element.Kind is not (WireframeKind.Checkbox or WireframeKind.Progress)) element.Indeterminate = false;
            if (element.Kind == WireframeKind.Checkbox && element.Indeterminate) element.Checked = false;
            if (!HasVisualState(element.Kind) || !Enum.IsDefined(element.VisualState)) element.VisualState = ControlVisualState.Normal;
            if (element.Kind != WireframeKind.Button && element.VisualState == ControlVisualState.Selected) element.VisualState = ControlVisualState.Normal;
            if (element.Kind == WireframeKind.Button && element.VisualState == ControlVisualState.Error) element.VisualState = ControlVisualState.Normal;
            if (element.Kind == WireframeKind.Radio && element.Checked && element.RadioGroup.Length > 0 && !checkedGroups.Add(element.RadioGroup))
                element.Checked = false;
        }
    }
}
