using System.Globalization;
using System.Text.RegularExpressions;
using Draftline.App.Model;
using Flare.Abstractions;
using Flare.Theming;

namespace Draftline.App.Theming;
/// <summary>Reads the registered library's token records; no theme values are duplicated here.</summary>
public static class MockupThemeResolver
{
    public static DrawingTheme? Resolve(IThemeService service, MockupThemeSelection selection, bool systemDark)
    {
        if (selection.ThemeId == "wireframe")
            return null;
        var theme = service.Themes.FirstOrDefault(t => t.Id == selection.ThemeId) ?? service.Themes.First(t => t.Id == "fluent2");
        var palette = service.Palettes.FirstOrDefault(p => p.Id == selection.PaletteId) ?? service.Palettes.First(p => p.Id == "fluent-blue");
        var colors = selection.Mode == "dark" || selection.Mode == "auto" && systemDark ? palette.Dark : palette.Light;
        var design = theme.Design;
        var variables = design.FlattenDesign();
        string Expand(string value)
        {
            for (var i = 0; i < 16 && value.Contains("var("); i++)
            {
                var next = Regex.Replace(value, @"var\((--[\w-]+)(?:,\s*([^()]+))?\)", m => variables.GetValueOrDefault(m.Groups[1].Value, m.Groups[2].Success ? m.Groups[2].Value : "0"));
                if (next == value)
                    break;
                value = next;
            }

            return value;
        }

        double Pixels(string value)
        {
            value = Expand(value).Trim();
            var calculation = Regex.Match(value, @"^calc\(\s*([\d.]+)(px|rem|em)?\s*([/*])\s*([\d.]+)\s*\)$");
            if (calculation.Success)
            {
                var left = double.Parse(calculation.Groups[1].Value, CultureInfo.InvariantCulture);
                var right = double.Parse(calculation.Groups[4].Value, CultureInfo.InvariantCulture);
                var unit = calculation.Groups[2].Value;
                var scale = unit == "rem" ? 16 : unit == "em" ? 12 : 1;
                return Math.Clamp((calculation.Groups[3].Value == "/" ? (right == 0 ? 0 : left / right) : left * right) * scale, 0, 9999);
            }

            value = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "0";
            var multiplier = value.EndsWith("rem", StringComparison.Ordinal) ? 16 : value.EndsWith("em", StringComparison.Ordinal) ? 12 : 1;
            var numeric = Regex.Match(value, @"^[\d.]+").Value;
            return double.TryParse(numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? Math.Clamp(number * multiplier, 0, 9999) : 0;
        }

        return new(colors.OnSurface, colors.Surface, colors.SurfaceContainer, colors.Outline, colors.Primary, colors.OnPrimary, colors.PrimaryContainer, Expand(design.Typography.BodyMedium.FontFamily), Pixels(design.Button.RadiusMd.TopLeft), Pixels(design.Input.OutlinedRadius), Pixels(design.Card.Radius));
    }
}
