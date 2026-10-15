using System.Text;
using Draftline.App.Model;

namespace Draftline.App.Services;

public static partial class SvgExporter
{
    /// <summary>A borderless SVG root for an individual low-fi canvas block.</summary>
    public static string Fragment(WireframeElement e)
    {
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {Number(e.Width)} {Number(e.Height)}\" width=\"100%\" height=\"100%\">");
        var drawing = e.Clone(); drawing.Disabled = false; drawing.Rotation = 0;
        WriteElement(svg,drawing,-e.X,-e.Y); svg.Append("</svg>"); return svg.ToString();
    }
    private static void WriteExtra(StringBuilder svg, WireframeElement e, double x, double y, double size)
    {
        var w=e.Width; var h=e.Height; var cx=x+w/2; var cy=y+h/2;
        if (ExtraElements.IsDiagram(e.Kind)) { WriteDiagram(svg,e,x,y,size); return; }
        if (ExtraElements.IsFrame(e.Kind)) { WriteFrame(svg,e,x,y,size); return; }
        switch(e.Kind)
        {
            case WireframeKind.Link:
                var labelX=x+(WireframeIcons.Find(e.IconKey) is null?2:24);
                WriteIcon(svg,e.IconKey,x+3,cy-8,16);
                Text(svg,labelX,cy+4,e.DisplayText,size,"400",1); Line(svg,labelX,cy+6,x+Math.Min(w-2,e.DisplayText.Length*size*.6+labelX-x),cy+6,1); break;
            case WireframeKind.Icon:
                if(WireframeIcons.Find(e.IconKey) is not null){var iconSize=Math.Min(w,h);WriteIcon(svg,e.IconKey,cx-iconSize/2,cy-iconSize/2,iconSize);break;}
                Text(svg,cx,cy+Math.Min(w,h)/3,e.DisplayText,Math.Min(w,h)*.8,"400",1,"middle"); break;
            case WireframeKind.Switch:
                Box(svg,x+1,cy-10,40,20,e.Checked ? "#e5e7eb" : Paper); Box(svg,x+(e.Checked ? 23 : 3),cy-8,16,16,e.Checked ? Ink : Paper);
                Text(svg,x+48,cy+4,e.DisplayText,size,"400",1); break;
            case WireframeKind.Stepper:
                Box(svg,x,y,w,h,Paper);Line(svg,x+30,y,x+30,y+h,1);Line(svg,x+w-30,y,x+w-30,y+h,1);
                Text(svg,x+15,cy+4,"−",size,"400",1,"middle");Text(svg,cx,cy+4,e.Value,size,"400",1,"middle");Text(svg,x+w-15,cy+4,"+",size,"400",1,"middle");break;
            case WireframeKind.Tooltip:
            case WireframeKind.Popover:
                Box(svg,x,y,w,Math.Min(28,h),Paper);Text(svg,x+6,y+18,e.DisplayText,size,"400",1);
                if(ElementState.IsExpanded(e) && h>28) { Box(svg,x,y+28,w,h-28,Paper);WriteLines(svg,e.SecondaryText,x+6,y+44,size,y+h-4); } break;
            case WireframeKind.DatePicker:
            case WireframeKind.TimePicker:
            case WireframeKind.ColorPicker:
                Box(svg,x,y,w,h,Paper);
                if(e.Kind==WireframeKind.ColorPicker) Box(svg,x+6,y+6,Math.Max(1,Math.Min(24,h-12)),Math.Max(1,h-12),ExtraElements.Color(e.Value));
                Text(svg,x+(e.Kind==WireframeKind.ColorPicker?38:6),cy+4,e.DisplayValue.Length>0?e.DisplayValue:e.DisplayText,size,"400",1);
                if(e.Kind!=WireframeKind.ColorPicker) Text(svg,x+w-16,cy+4,e.Kind==WireframeKind.DatePicker?"▦":"◷",size,"400",1);break;
        }
    }
}
