using System.Globalization;
using System.Xml.Linq;
using Draftline.App.Model;

namespace Draftline.App.Services;
public static partial class SvgExporter
{
    /// <summary>Apply the same resolved drawing tokens to every export format.</summary>
    public static string ApplyTheme(string svg,DrawingTheme? theme)
    {
        if(theme is null)return svg;
        var document=XDocument.Parse(svg);
        foreach(var element in document.Descendants())
        {
            var block=element.AncestorsAndSelf().FirstOrDefault(e=>e.Attribute("data-kind") is not null);
            foreach(var attribute in element.Attributes().ToList())
            {
                if(attribute.Name.LocalName=="font-family")attribute.Value=theme.FontFamily;
                else if(attribute.Name.LocalName is "fill" or "stroke")
                {
                    if(block?.Attribute("data-author-color")?.Value.Equals(attribute.Value,StringComparison.OrdinalIgnoreCase)==true)continue;
                    attribute.Value=attribute.Value.ToLowerInvariant() switch
                    {
                        "#1b1e24"=>theme.Ink,"#fff" or "#ffffff"=>theme.Paper,"#e5e7eb"=>theme.Selected,
                        "#d1d5db"=>theme.Outline,"#9ca3af"=>theme.Outline,"#f3f4f6"=>theme.Surface,_=>attribute.Value
                    };
                }
            }
        }
        foreach(var group in document.Descendants().Where(e=>e.Name.LocalName=="g" && e.Attribute("data-kind") is not null))
        {
            var kind=group.Attribute("data-kind")!.Value;var rectangles=group.Descendants().Where(e=>e.Name.LocalName=="rect").ToList();
            if(rectangles.FirstOrDefault() is { } frame && frame.Attribute("stroke")?.Value==theme.Ink)frame.SetAttributeValue("stroke",theme.Outline);
            var radius=kind=="Button"?theme.ButtonRadius:kind is "TextField" or "TextArea" or "Select" or "DatePicker" or "TimePicker" or "ColorPicker"?theme.FieldRadius:
                kind is "Panel" or "Tabs" or "Accordion" or "Alert" or "Popover" or "Tooltip" or "Fieldset"?theme.PanelRadius:0;
            if(radius>0 && rectangles.FirstOrDefault() is { } box){box.SetAttributeValue("rx",radius.ToString("0.###",CultureInfo.InvariantCulture));box.SetAttributeValue("ry",radius.ToString("0.###",CultureInfo.InvariantCulture));}
            if(kind is "TextField" or "TextArea" or "Select")rectangles.FirstOrDefault()?.SetAttributeValue("fill",theme.Surface);
            if(kind=="Button")
            {
                if(rectangles.FirstOrDefault() is { } buttonBox){buttonBox.SetAttributeValue("fill",theme.Primary);buttonBox.SetAttributeValue("stroke",theme.Primary);}
                foreach(var text in group.Descendants().Where(e=>e.Name.LocalName=="text"))text.SetAttributeValue("fill",theme.OnPrimary);
                foreach(var path in group.Descendants().Where(e=>e.Name.LocalName=="path"))path.SetAttributeValue("stroke",theme.OnPrimary);
            }
            if(kind=="Checkbox" && group.Descendants().Any(e=>e.Name.LocalName=="line"))
            {
                rectangles.FirstOrDefault()?.SetAttributeValue("fill",theme.Primary);
                rectangles.FirstOrDefault()?.SetAttributeValue("stroke",theme.Primary);
                foreach(var line in group.Descendants().Where(e=>e.Name.LocalName=="line"))line.SetAttributeValue("stroke",theme.OnPrimary);
            }
            if(kind=="Radio" && group.Descendants().Count(e=>e.Name.LocalName=="circle")>1)
                group.Descendants().Last(e=>e.Name.LocalName=="circle").SetAttributeValue("fill",theme.Primary);
            if(kind is "Slider" or "Progress")
                foreach(var shape in group.Descendants().Where(e=>e.Attribute("fill")?.Value==theme.Ink))shape.SetAttributeValue("fill",theme.Primary);
        }
        return document.ToString(SaveOptions.DisableFormatting);
    }
}
