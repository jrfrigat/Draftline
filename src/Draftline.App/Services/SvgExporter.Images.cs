using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;
public static partial class SvgExporter
{
    private static void WriteImage(StringBuilder svg, WireframeElement e, double x, double y)
    {
        var preserve = e.ImageFit == "stretch" ? "none" : e.ImageFit == "cover" ? "xMidYMid slice" : "xMidYMid meet";
        svg.Append($"<svg x=\"{Number(x)}\" y=\"{Number(y)}\" width=\"{Number(e.Width)}\" height=\"{Number(e.Height)}\" viewBox=\"{Number(e.CropX * e.ImageWidth)} {Number(e.CropY * e.ImageHeight)} {Number(e.CropWidth * e.ImageWidth)} {Number(e.CropHeight * e.ImageHeight)}\" preserveAspectRatio=\"{preserve}\" overflow=\"hidden\">");
        var clip = "image-" + Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(e.Id));
        svg.Append($"<defs><clipPath id=\"{clip}\"><rect x=\"{Number(e.CropX * e.ImageWidth)}\" y=\"{Number(e.CropY * e.ImageHeight)}\" width=\"{Number(e.CropWidth * e.ImageWidth)}\" height=\"{Number(e.CropHeight * e.ImageHeight)}\" /></clipPath></defs>");
        svg.Append($"<image x=\"0\" y=\"0\" width=\"{Number(e.ImageWidth)}\" height=\"{Number(e.ImageHeight)}\" clip-path=\"url(#{clip})\" href=\"{System.Security.SecurityElement.Escape(e.ImageData)}\" /></svg>");
    }
}
