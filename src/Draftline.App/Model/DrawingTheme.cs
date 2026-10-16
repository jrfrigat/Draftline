using System.Globalization;

namespace Draftline.App.Model;
/// <summary>Resolved Flare tokens shared by the drawing and its standalone exports.</summary>
public sealed record DrawingTheme(string Ink, string Paper, string Surface, string Outline, string Primary, string OnPrimary, string Selected, string FontFamily, double ButtonRadius, double FieldRadius, double PanelRadius)
{
    public string Css => $"--df-ink:{Ink};--df-paper:{Paper};--df-surface:{Surface};--df-outline:{Outline};--df-primary:{Primary};--df-on-primary:{OnPrimary};--df-selected:{Selected};--df-font:{FontFamily};--df-button-radius:{N(ButtonRadius)}px;--df-field-radius:{N(FieldRadius)}px;--df-panel-radius:{N(PanelRadius)}px;";

    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
