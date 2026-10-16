using System.Globalization;
using System.Xml.Linq;
using Draftline.App.Model;
using Draftline.App.Services;

namespace Draftline.App.Tests;

public sealed class DropdownExportTests
{
    [Theory]
    [InlineData("", 80, false)]
    [InlineData("Короткое", 200, false)]
    [InlineData("Очень длинная подпись выпадающего списка", 80, true)]
    public void Label_is_clipped_before_the_trailing_arrow(string value, double width, bool expanded)
    {
        var element = ElementDefaults.Create(WireframeKind.Select);
        element.Value = value;
        element.Width = width;
        element.Expanded = expanded;
        var svg = XDocument.Parse(SvgExporter.ExportElement(element));
        var arrow = Assert.Single(svg.Descendants(), n => n.Name.LocalName == "text" && n.Value == (expanded ? "▴" : "▾"));
        var label = Assert.Single(svg.Descendants(), n => n.Name.LocalName == "svg" && (string?)n.Attribute("overflow") == "hidden");
        static double Number(XElement node, string attribute) => double.Parse(node.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);
        Assert.Equal(width - 16, Number(arrow, "x"));
        Assert.True(Number(label, "x") + Number(label, "width") < Number(arrow, "x"));
        Assert.Equal(value, Assert.Single(label.Elements()).Value);
    }
}
