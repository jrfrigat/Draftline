using Flare.Abstractions.Tokens;
using Flare.Theme.FluentUI2;

namespace Flare.Theme.OneC;

/// <summary>Yellow accents and neutral work surfaces for the OneC theme.</summary>
public static class OneCPalettes
{
    /// <summary>Stable palette identifier.</summary>
    public const string YellowId = "onec-yellow";
    /// <summary>Complete light and dark schemes; no external color resources are needed.</summary>
    public static Palette Yellow { get; } = new()
    {
        Id = YellowId, Name = "Жёлтая 1С", Source = "1С · Такси",
        Light = Scheme(false), Dark = Scheme(true)
    };

    private static ColorScheme Scheme(bool dark) => (dark ? Fluent2Palettes.Blue.Dark : Fluent2Palettes.Blue.Light) with
    {
        Primary = "#FFDE3F", OnPrimary = "#1F1F1F",
        PrimaryContainer = dark ? "#514400" : "#FFF3B0", OnPrimaryContainer = dark ? "#FFF3B0" : "#393000",
        Secondary = dark ? "#E5CC68" : "#6F6000", OnSecondary = dark ? "#292100" : "#FFFFFF",
        SecondaryContainer = dark ? "#494129" : "#F2E9C8", OnSecondaryContainer = dark ? "#F2E9C8" : "#393322",
        Tertiary = dark ? "#E4C999" : "#765A28", OnTertiary = dark ? "#332300" : "#FFFFFF",
        TertiaryContainer = dark ? "#4E3E22" : "#F5E6CC", OnTertiaryContainer = dark ? "#F5E6CC" : "#3E2E13",
        PrimaryFixed = "#FFF3B0", PrimaryFixedDim = "#FFDE3F", OnPrimaryFixed = "#1F1F1F", OnPrimaryFixedVariant = "#514400",
        SecondaryFixed = "#F2E9C8", SecondaryFixedDim = "#E5CC68", OnSecondaryFixed = "#292100", OnSecondaryFixedVariant = "#494129",
        TertiaryFixed = "#F5E6CC", TertiaryFixedDim = "#E4C999", OnTertiaryFixed = "#332300", OnTertiaryFixedVariant = "#4E3E22",
        Surface = dark ? "#242424" : "#FFFFFF", OnSurface = dark ? "#F2F2F2" : "#242424",
        SurfaceVariant = dark ? "#333333" : "#F2F2F2", OnSurfaceVariant = dark ? "#D0D0D0" : "#505050",
        Background = dark ? "#1C1C1C" : "#F5F5F5", OnBackground = dark ? "#F2F2F2" : "#242424",
        SurfaceContainerLowest = dark ? "#191919" : "#FFFFFF", SurfaceContainerLow = dark ? "#202020" : "#FAFAFA",
        SurfaceContainer = dark ? "#2C2C2C" : "#F3F3F3", SurfaceContainerHigh = dark ? "#363636" : "#EDEDED",
        SurfaceContainerHighest = dark ? "#414141" : "#E5E5E5",
        Outline = dark ? "#909090" : "#929292", OutlineVariant = dark ? "#606060" : "#C7C7C7",
        InverseSurface = dark ? "#F2F2F2" : "#303030", InverseOnSurface = dark ? "#242424" : "#F2F2F2",
        InversePrimary = dark ? "#6F6000" : "#FFDE3F"
    };
}
