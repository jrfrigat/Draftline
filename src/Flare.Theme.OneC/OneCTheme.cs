using Flare.Abstractions;
using Flare.Abstractions.Tokens;
using Flare.Abstractions.Tokens.Components;
using Flare.Theme.FluentUI2;

namespace Flare.Theme.OneC;

/// <summary>An unofficial compact theme inspired by the 1C Taxi interface.</summary>
public sealed class OneCTheme : ITheme
{
    /// <summary>Stable theme identifier.</summary>
    public const string ThemeId = "onec";
    private static readonly FluentUI2Theme Foundation = new();
    /// <inheritdoc />
    public string Id => ThemeId;
    /// <inheritdoc />
    public string DisplayName => "1С · Такси";
    /// <inheritdoc />
    public ITheme Base => Foundation;
    /// <inheritdoc />
    public DesignTokens Design { get; } = CreateDesign();
    /// <inheritdoc />
    public string DefaultPaletteId => OneCPalettes.YellowId;
    /// <inheritdoc />
    public IReadOnlyList<Palette> Palettes => [OneCPalettes.Yellow];
    /// <inheritdoc />
    public IReadOnlyList<string> StyleAssets => Foundation.StyleAssets;
    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? ExtendedDarkOverride => Foundation.ExtendedDarkOverride;

    private static DesignTokens CreateDesign()
    {
        var source = Foundation.Design;
        static TypeStyle Font(TypeStyle style) => style with { FontFamily = "Arial, sans-serif" };
        var type = source.Typography;
        var radius = CornerRadiusTokens.All("2px");
        return source with
        {
            Typography = type with
            {
                DisplayLarge = Font(type.DisplayLarge), DisplayMedium = Font(type.DisplayMedium), DisplaySmall = Font(type.DisplaySmall),
                HeadlineLarge = Font(type.HeadlineLarge), HeadlineMedium = Font(type.HeadlineMedium), HeadlineSmall = Font(type.HeadlineSmall),
                TitleLarge = Font(type.TitleLarge), TitleMedium = Font(type.TitleMedium), TitleSmall = Font(type.TitleSmall),
                BodyLarge = Font(type.BodyLarge), BodyMedium = Font(type.BodyMedium), BodySmall = Font(type.BodySmall),
                LabelLarge = Font(type.LabelLarge), LabelMedium = Font(type.LabelMedium), LabelSmall = Font(type.LabelSmall)
            },
            Shape = source.Shape with { ExtraSmall = "1px", Small = "2px", Medium = "2px", Large = "3px", ExtraLarge = "4px" },
            Button = source.Button with
            {
                ContainerRadius = "2px", RadiusXs = radius, RadiusSm = radius, RadiusMd = radius, RadiusLg = radius, RadiusXl = radius,
                IconOnlyRadiusXs = "2px", IconOnlyRadiusSm = "2px", IconOnlyRadiusMd = "2px", IconOnlyRadiusLg = "2px", IconOnlyRadiusXl = "2px",
                HeightXs = "24px", HeightSm = "28px", HeightMd = "32px", HeightLg = "36px", HeightXl = "40px",
                LabelXs = Font(source.Button.LabelXs), LabelSm = Font(source.Button.LabelSm), LabelMd = Font(source.Button.LabelMd),
                LabelLg = Font(source.Button.LabelLg), LabelXl = Font(source.Button.LabelXl)
            },
            Input = source.Input with
            {
                OutlinedRadius = "2px", OutlinedVariantRadius = "2px", FilledVariantRadius = "2px",
                HeightXs = "24px", HeightSm = "28px", HeightMd = "32px", HeightLg = "36px", HeightXl = "40px"
            },
            Card = source.Card with { Radius = "2px" }
        };
    }
}
