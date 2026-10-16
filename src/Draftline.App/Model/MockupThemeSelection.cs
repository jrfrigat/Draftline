namespace Draftline.App.Model;
/// <summary>Portable appearance of the drawing, independent of the editor's local theme.</summary>
public sealed class MockupThemeSelection
{
    public string ThemeId { get; set; } = "wireframe";
    public string PaletteId { get; set; } = "fluent-blue";
    public string Mode { get; set; } = "light";

    public MockupThemeSelection Clone() => new()
    {
        ThemeId = ThemeId,
        PaletteId = PaletteId,
        Mode = Mode
    };
    public void Normalize()
    {
        if (ThemeId is not ("wireframe" or "fluent2" or "md3-expressive"))
            ThemeId = "wireframe";
        if (PaletteId is not ("fluent-blue" or "fluent-excel" or "fluent-teams"))
            PaletteId = "fluent-blue";
        if (Mode is not ("light" or "dark" or "auto"))
            Mode = "light";
    }
}
