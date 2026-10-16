using System.Text.Json;
using Draftline.App.Model;

namespace Draftline.App.Services;

/// <summary>One view's last SVG, independent of placement. Image data is compared without copying or encoding it.</summary>
public sealed class ElementFragmentCache
{
    private string? _key, _image, _svg;
    private DrawingTheme? _theme;

    public string Render(WireframeElement element, DrawingTheme? theme)
    {
        var local = element.Clone();
        local.X = local.Y = local.Rotation = local.ComponentOriginX = local.ComponentOriginY = 0;
        local.ZIndex = 0;
        local.ComponentBaseline = "";
        local.Disabled = false;
        local.ImageData = "";
        var key = JsonSerializer.Serialize(local, DraftlineJsonContext.Default.WireframeElement);
        if (_key == key && _image == element.ImageData && _theme == theme) return _svg!;
        local.ImageData = element.ImageData;
        _svg = SvgExporter.Fragment(local, theme);
        (_key, _image, _theme) = (key, element.ImageData, theme);
        return _svg;
    }
}
