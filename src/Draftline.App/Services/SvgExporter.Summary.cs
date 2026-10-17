using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;
public static partial class SvgExporter
{
    // A nested viewport clips long authored text to the block, including in PNG/PDF exports.
    private static void WriteSummary(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        svg.Append($"<svg x=\"{Number(x)}\" y=\"{Number(y)}\" width=\"{Number(e.Width)}\" height=\"{Number(e.Height)}\" viewBox=\"0 0 {Number(e.Width)} {Number(e.Height)}\" overflow=\"hidden\">");
        if (e.Kind == WireframeKind.Stat)
        {
            Text(svg, 12, 20, e.DisplayText, size, "400", .7);
            Text(svg, 12, 52, e.Value, size * 2, "700", 1);
            var unitX = 12 + e.Value.Length * size * 1.2;
            Text(svg, unitX + 8, 52, e.StatUnit, size, "400", .7);
            Text(svg, 12, 74, e.StatDelta, size, "700", .8);
            Text(svg, 12, 96, e.SecondaryText, size * .9, "400", .7);
        }
        else
        {
            Text(svg, 12, 20, e.HeaderEyebrow, size * .85, "400", .7);
            Text(svg, 12, 52, e.DisplayText, size * 1.8, "700", 1);
            Text(svg, 12, 80, e.SecondaryText, size, "400", .7);
            if (!string.IsNullOrEmpty(e.HeaderAction))
            {
                var width = SummaryBlocks.ActionBounds(e).Width;
                svg.Append("<g data-kind=\"Button\">");
                Box(svg, 12, 104, width, 32, Paper);
                Text(svg, 12 + width / 2, 125, e.HeaderAction, size, "400", 1, "middle");
                svg.Append("</g>");
            }
            Line(svg, 0, e.Height - 1, e.Width, e.Height - 1, .3);
        }
        svg.Append("</svg>");
    }
}
