using System.Xml.Linq;
using Draftline.App.Model;
using Draftline.App.Services;
using Draftline.App.Theming;
using Flare.Abstractions;
using Flare.Abstractions.Tokens;
using Flare.Theme.FluentUI2;
using Flare.Theme.MaterialDesign3Expressive;
using Flare.Theme.OneC;
using Flare.Theming;
using Microsoft.JSInterop;

namespace Draftline.App.Tests;

public sealed class OneCThemeTests
{
    [Fact]
    public void Package_provides_a_valid_complete_theme_without_app_dependencies()
    {
        var theme = new OneCTheme();
        Assert.Empty(new ThemeValidator().Validate(theme));
        Assert.True(theme.Design.FlattenDesign().Count > 100);
        Assert.DoesNotContain(theme.GetType().Assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Draftline"));
        Assert.Equal(OneCPalettes.YellowId, theme.DefaultPaletteId);
        Assert.Single(theme.Palettes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Main_color_pairs_have_readable_text(bool dark)
    {
        var colors = dark ? OneCPalettes.Yellow.Dark : OneCPalettes.Yellow.Light;
        Assert.True(Contrast(colors.Primary, colors.OnPrimary) >= 4.5);
        Assert.True(Contrast(colors.Surface, colors.OnSurface) >= 4.5);
        Assert.True(Contrast(colors.PrimaryContainer, colors.OnPrimaryContainer) >= 4.5);
        Assert.True(Contrast(colors.Secondary, colors.OnSecondary) >= 4.5);
    }

    [Fact]
    public void OneC_selection_survives_JSON_and_leaving_it_restores_a_compatible_palette()
    {
        var document = DraftDocument.CreateNew("1С");
        document.MockupTheme = new() { ThemeId = "onec", Mode = "dark", PaletteId = "fluent-excel" };
        document.Normalize();
        var restored = DraftlineJson.Read(DraftlineJson.Serialize(document)).Document!;
        Assert.Equal("onec", restored.MockupTheme.ThemeId);
        Assert.Equal("onec-yellow", restored.MockupTheme.PaletteId);
        Assert.Equal("dark", restored.MockupTheme.Mode);
        restored.MockupTheme.ThemeId = "fluent2";
        restored.MockupTheme.Normalize();
        Assert.Equal("fluent-blue", restored.MockupTheme.PaletteId);
    }

    [Theory]
    [InlineData("light", "#FFFFFF")]
    [InlineData("dark", "#242424")]
    public void Drawing_and_SVG_resolve_the_packages_own_tokens(string mode, string paper)
    {
        var service = Service();
        var drawing = MockupThemeResolver.Resolve(service, new() { ThemeId = "onec", PaletteId = "onec-yellow", Mode = mode }, false)!;
        Assert.Equal(paper, drawing.Paper);
        Assert.Equal("#FFDE3F", drawing.Primary);
        Assert.Equal(2, drawing.ButtonRadius);
        Assert.Equal(2, drawing.FieldRadius);
        Assert.Equal("Arial, sans-serif", drawing.FontFamily);
        var button = ElementDefaults.Create(WireframeKind.Button);
        var svg = XDocument.Parse(SvgExporter.ApplyTheme(SvgExporter.ExportElement(button), drawing));
        Assert.Contains(svg.Descendants(), e => e.Name.LocalName == "rect" && (string?)e.Attribute("fill") == drawing.Primary);
        Assert.Contains(svg.Descendants(), e => e.Name.LocalName == "text" && (string?)e.Attribute("fill") == drawing.OnPrimary);
    }

    [Fact]
    public async Task Drawing_themes_cannot_replace_the_Fluent_shell()
    {
        var service = Service();
        await using var coordinator = new ThemeCoordinator(service, new UnusedJs());
        await service.SetThemeAsync("md3-expressive");
        Assert.Equal("draftline-fluent2", service.CurrentTheme.Id);
        await service.SetThemeAsync("onec");
        Assert.Equal("draftline-fluent2", service.CurrentTheme.Id);
        var documentTheme = new MockupThemeSelection { ThemeId = "onec", PaletteId = "onec-yellow", Mode = "light" };
        Assert.Equal("#FFDE3F", coordinator.Drawing(documentTheme)!.Primary);
        await service.SetPaletteAsync("fluent-excel");
        Assert.Equal("fluent-excel", service.CurrentPalette.Id);
        Assert.Equal("#FFDE3F", coordinator.Drawing(documentTheme)!.Primary);
    }

    private static ThemeService Service()
    {
        var service = new ThemeService(new NullInjector(), DraftlineTheme.Build(), Fluent2Palettes.Blue);
        service.RegisterTheme(new FluentUI2Theme());
        service.RegisterTheme(new MaterialDesign3ExpressiveTheme());
        service.RegisterTheme(new OneCTheme());
        service.RegisterPalette(OneCPalettes.Yellow);
        service.RegisterPalette(Fluent2Palettes.Excel);
        return service;
    }

    private static double Contrast(string a, string b)
    {
        static double Luminance(string color)
        {
            double Linear(int offset)
            {
                var value = Convert.ToInt32(color.Substring(offset, 2), 16) / 255d;
                return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
            }
            return .2126 * Linear(1) + .7152 * Linear(3) + .0722 * Linear(5);
        }
        var x = Luminance(a); var y = Luminance(b);
        return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05);
    }

    private sealed class NullInjector : ICssVariableInjector
    {
        public ValueTask InjectAsync(DesignTokens design, ColorScheme colors, CancellationToken ct = default) => ValueTask.CompletedTask;
        public ValueTask SetStaticCssAsync(string css, CancellationToken ct = default) => ValueTask.CompletedTask;
        public ValueTask SetCustomTokensAsync(IReadOnlyDictionary<string, string> tokens, CancellationToken ct = default) => ValueTask.CompletedTask;
        public ValueTask ClearCustomTokensAsync(IEnumerable<string> tokenNames, CancellationToken ct = default) => ValueTask.CompletedTask;
    }

    private sealed class UnusedJs : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }
}
