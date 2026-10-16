using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;
public static partial class SvgExporter
{
    private static void WriteIcon(StringBuilder svg, string key, double x, double y, double size)
    {
        if (WireframeIcons.Find(key)is not { } icon)
            return;
        svg.Append($"<svg x=\"{Number(x)}\" y=\"{Number(y)}\" width=\"{Number(size)}\" height=\"{Number(size)}\" viewBox=\"0 0 24 24\"><path d=\"{icon.Path}\" fill=\"none\" stroke=\"{Ink}\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" /></svg>");
    }
}
