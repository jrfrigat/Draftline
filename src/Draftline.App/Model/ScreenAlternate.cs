namespace Draftline.App.Model;
/// <summary>A named, independently saved design of one screen, not another navigation destination.</summary>
public sealed class ScreenAlternate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "Вариант";
    public double CanvasWidth { get; set; }
    public double CanvasHeight { get; set; }
    public List<WireframeElement> Elements { get; set; } = new();

    public ScreenAlternate Clone() => new()
    {
        Id = Id,
        Name = Name,
        CanvasWidth = CanvasWidth,
        CanvasHeight = CanvasHeight,
        Elements = Elements.Select(e => e.Clone()).ToList()
    };
    public void Capture(DraftLayer layer)
    {
        CanvasWidth = layer.CanvasWidth;
        CanvasHeight = layer.CanvasHeight;
        Elements = layer.Elements.Select(e => e.Clone()).ToList();
    }

    public void Apply(DraftLayer layer)
    {
        layer.CanvasWidth = CanvasWidth;
        layer.CanvasHeight = CanvasHeight;
        layer.Elements = Elements.Select(e => e.Clone()).ToList();
    }
}
