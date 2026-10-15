using System.Globalization;

namespace Draftline.App.Model;

/// <summary>Finite slider range, step and drawing percentage.</summary>
public static class ControlRange
{
    /// <summary>Effective numeric value of a progress bar or slider.</summary>
    public static double Value(WireframeElement e) => double.TryParse(e.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n) ? n : 60;
    /// <summary>Snap to a valid step originating at the minimum.</summary>
    public static double Snap(WireframeElement e, double value)
    {
        var count = Math.Floor((e.Maximum - e.Minimum) / e.Step + 1e-10);
        return e.Minimum + Math.Clamp(Math.Round((value - e.Minimum) / e.Step, MidpointRounding.AwayFromZero), 0, count) * e.Step;
    }
    /// <summary>Percentage used by canvas and SVG.</summary>
    public static double Percent(WireframeElement e) => e.Kind is WireframeKind.Slider or WireframeKind.Stepper
        ? Math.Clamp((Value(e) - e.Minimum) / Math.Max(1e-6, e.Maximum - e.Minimum) * 100, 0, 100)
        : Math.Clamp(Value(e), 0, 100);
    /// <summary>Safe range and a value aligned with its step.</summary>
    public static void Normalize(WireframeElement e)
    {
        if (e.Kind is not (WireframeKind.Progress or WireframeKind.Slider or WireframeKind.Stepper)) return;
        e.Minimum = double.IsFinite(e.Minimum) ? Math.Clamp(e.Minimum, -1e9, 1e9 - 1) : 0;
        e.Maximum = double.IsFinite(e.Maximum) ? Math.Clamp(e.Maximum, -1e9, 1e9) : 100;
        if (e.Maximum <= e.Minimum) e.Maximum = e.Minimum + 1;
        if (e.Maximum - e.Minimum < 1e-6) e.Maximum = e.Minimum + 1e-6;
        e.Step = Math.Clamp(double.IsFinite(e.Step) ? e.Step : 1, 1e-6, e.Maximum - e.Minimum);
        e.Value = (e.Kind is WireframeKind.Slider or WireframeKind.Stepper ? Snap(e, Value(e)) : Percent(e)).ToString("G", CultureInfo.InvariantCulture);
    }
}
