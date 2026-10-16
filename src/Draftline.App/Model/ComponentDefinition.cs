namespace Draftline.App.Model;
/// <summary>A reusable source, independent of any screen or instance identities.</summary>
public sealed class ComponentDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("n");
    public string Name { get; set; } = "Компонент";
    public List<WireframeElement> Elements { get; set; } = new();

    public ComponentDefinition Clone() => new()
    {
        Id = Id,
        Name = Name,
        Elements = Elements.Select(e => e.Clone()).ToList()
    };
    public static void Detach(WireframeElement e)
    {
        e.ComponentKey = e.InstanceKey = e.SourceKey = e.ComponentBaseline = "";
        e.ComponentOriginX = e.ComponentOriginY = 0;
    }
}
