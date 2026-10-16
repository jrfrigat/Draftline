using System.Text.Json;
using System.Text.Json.Nodes;
using Draftline.App.Model;

namespace Draftline.App.Services;
public sealed partial class DocumentSession
{
    public string SelectedInstanceKey => Selection.Select(id => CurrentLayer.Find(id)?.InstanceKey ?? "").Where(key => key.Length > 0).Distinct().ToList()is { Count: 1 } keys ? keys[0] : "";

    private static string Snapshot(WireframeElement e) => JsonSerializer.Serialize(e, DraftlineJsonContext.Default.WireframeElement);
    private static WireframeElement ReadSnapshot(string text, WireframeElement fallback)
    {
        try
        {
            return JsonSerializer.Deserialize(text, DraftlineJsonContext.Default.WireframeElement) ?? fallback.Clone();
        }
        catch (JsonException)
        {
            return fallback.Clone();
        }
    }

    private static List<WireframeElement> Templates(IReadOnlyList<WireframeElement> elements, double x, double y, bool preserveSource = true)
    {
        var ids = elements.ToDictionary(e => e.Id, e => preserveSource && e.SourceKey.Length > 0 ? e.SourceKey : Guid.NewGuid().ToString("n"));
        return elements.Select(e =>
        {
            var copy = e.Clone();
            copy.Id = ids[e.Id];
            copy.X -= x;
            copy.Y -= y;
            ComponentDefinition.Detach(copy);
            copy.ParentTabsId = ids.GetValueOrDefault(e.ParentTabsId, "");
            return copy;
        }).ToList();
    }

    /// <summary>Save a selection as a source and turn it into its first instance.</summary>
    public void CreateComponent(string name)
    {
        var elements = Members();
        if (elements.Count == 0)
            return;
        var bounds = ElementGroups.Bounds(elements.Select(e => e.Bounds));
        var templates = Templates(elements, bounds.X, bounds.Y, false);
        var definition = new ComponentDefinition
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Компонент" : name.Trim(),
            Elements = templates
        };
        var instance = Guid.NewGuid().ToString("n");
        var group = Guid.NewGuid().ToString("n");
        Edit("Компонент создан", _ =>
        {
            Document.Components.Add(definition);
            for (var i = 0; i < elements.Count; i++)
            {
                var e = elements[i];
                e.ComponentKey = definition.Id;
                e.InstanceKey = instance;
                e.SourceKey = templates[i].Id;
                e.ComponentOriginX = bounds.X;
                e.ComponentOriginY = bounds.Y;
                e.ComponentBaseline = Snapshot(templates[i]);
                e.GroupPath.Insert(0, group);
            }
        });
        SetSelection(elements.Select(e => e.Id));
    }

    private List<WireframeElement> Materialize(ComponentDefinition definition, double x, double y, string? instance = null)
    {
        var ids = definition.Elements.ToDictionary(e => e.Id, _ => Guid.NewGuid().ToString("n"));
        var groups = new Dictionary<string, string>();
        var root = Guid.NewGuid().ToString("n");
        instance ??= Guid.NewGuid().ToString("n");
        return definition.Elements.Select(template =>
        {
            var e = template.Clone();
            e.Id = ids[template.Id];
            e.X += x;
            e.Y += y;
            e.ParentTabsId = ids.GetValueOrDefault(template.ParentTabsId, "");
            e.GroupPath = new()
            {
                root
            };
            foreach (var group in template.GroupPath)
            {
                if (!groups.TryGetValue(group, out var next))
                    groups[group] = next = Guid.NewGuid().ToString("n");
                e.GroupPath.Add(next);
            }

            e.ComponentKey = definition.Id;
            e.InstanceKey = instance;
            e.SourceKey = template.Id;
            e.ComponentBaseline = Snapshot(template);
            e.ComponentOriginX = x;
            e.ComponentOriginY = y;
            return e;
        }).ToList();
    }

    public void InsertComponent(string id)
    {
        if (Document.Components.FirstOrDefault(c => c.Id == id)is not { } definition)
            return;
        var elements = Materialize(definition, 48, 48);
        var bounds = ElementGroups.Bounds(elements.Select(e => e.Bounds));
        var delta = TabPanels.ClampTranslation(elements.Select(e => e.Bounds), 0, 0, CurrentLayer.CanvasWidth, CurrentLayer.CanvasHeight);
        Edit("Экземпляр добавлен", _ =>
        {
            foreach (var e in elements)
            {
                e.X += delta.X;
                e.Y += delta.Y;
                e.ComponentOriginX += delta.X;
                e.ComponentOriginY += delta.Y;
                e.ZIndex = CurrentLayer.NextZIndex();
                CurrentLayer.Elements.Add(e);
            }
        });
        EditingGroupId = "";
        SetSelection(elements.Select(e => e.Id));
    }

    private static readonly HashSet<string> InstanceFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "id",
        "groupPath",
        "parentTabsId",
        "parentTabIndex",
        "zIndex",
        "componentKey",
        "instanceKey",
        "sourceKey",
        "componentBaseline",
        "componentOriginX",
        "componentOriginY"
    };
    private static WireframeElement MergeTemplate(WireframeElement current, WireframeElement baseline, WireframeElement source, bool reset)
    {
        var local = current.Clone();
        ComponentDefinition.Detach(local);
        local.X -= current.ComponentOriginX;
        local.Y -= current.ComponentOriginY;
        var values = JsonNode.Parse(Snapshot(local))!.AsObject();
        var before = JsonNode.Parse(Snapshot(baseline))!.AsObject();
        var next = JsonNode.Parse(Snapshot(source))!.AsObject();
        foreach (var field in next)
            if (!InstanceFields.Contains(field.Key) && (reset || JsonNode.DeepEquals(values[field.Key], before[field.Key])))
                values[field.Key] = field.Value?.DeepClone();
        var merged = JsonSerializer.Deserialize(values.ToJsonString(), DraftlineJsonContext.Default.WireframeElement)!;
        merged.X += current.ComponentOriginX;
        merged.Y += current.ComponentOriginY;
        merged.Id = current.Id;
        merged.GroupPath = new(current.GroupPath);
        merged.ParentTabsId = current.ParentTabsId;
        merged.ParentTabIndex = current.ParentTabIndex;
        merged.ZIndex = current.ZIndex;
        merged.ComponentKey = current.ComponentKey;
        merged.InstanceKey = current.InstanceKey;
        merged.SourceKey = source.Id;
        merged.ComponentBaseline = Snapshot(source);
        merged.ComponentOriginX = current.ComponentOriginX;
        merged.ComponentOriginY = current.ComponentOriginY;
        return merged;
    }

    /// <summary>Promote the selected instance to the source and update unmodified fields elsewhere.</summary>
    public void UpdateComponentFromSelection()
    {
        var key = SelectedInstanceKey;
        if (key.Length == 0)
            return;
        var originals = CurrentLayer.Elements.Where(e => e.InstanceKey == key).ToList();
        if (originals.Count == 0)
            return;
        var first = originals[0];
        if (Document.Components.FirstOrDefault(c => c.Id == first.ComponentKey)is not { } definition)
            return;
        var members = Members();
        var sourceMembers = members.Any(e => e.InstanceKey != key) ? members : originals;
        var templates = Templates(sourceMembers, first.ComponentOriginX, first.ComponentOriginY);
        var old = definition.Elements.ToDictionary(e => e.Id, e => e.Clone());
        Edit("Основа компонента обновлена", _ =>
        {
            definition.Elements = templates;
            for (var i = 0; i < sourceMembers.Count; i++)
                if (sourceMembers[i].InstanceKey != key)
                {
                    var added = sourceMembers[i];
                    added.ComponentKey = definition.Id;
                    added.InstanceKey = key;
                    added.SourceKey = templates[i].Id;
                    added.ComponentOriginX = first.ComponentOriginX;
                    added.ComponentOriginY = first.ComponentOriginY;
                    added.ComponentBaseline = Snapshot(templates[i]);
                    added.GroupPath = new(first.GroupPath);
                }

            foreach (var elements in Document.ScreenElementLists)
                foreach (var instance in elements.Where(e => e.ComponentKey == definition.Id).GroupBy(e => e.InstanceKey).Select(g => g.ToList()).ToList())
                {
                    var origin = instance[0];
                    var materialized = Materialize(definition, origin.ComponentOriginX, origin.ComponentOriginY, origin.InstanceKey);
                    var mapping = instance.ToDictionary(e => e.SourceKey, e => e.Id);
                    foreach (var added in materialized)
                        mapping.TryAdd(added.SourceKey, added.Id);
                    foreach (var current in instance)
                    {
                        var source = templates.FirstOrDefault(e => e.Id == current.SourceKey);
                        if (source is null)
                        {
                            elements.Remove(current);
                            continue;
                        }

                        var fallback = old.GetValueOrDefault(current.SourceKey, source);
                        var merged = MergeTemplate(current, ReadSnapshot(current.ComponentBaseline, fallback), source, ReferenceEquals(elements, CurrentLayer.Elements) && current.InstanceKey == key);
                        merged.ParentTabsId = mapping.GetValueOrDefault(source.ParentTabsId, "");
                        elements[elements.IndexOf(current)] = merged;
                    }

                    foreach (var added in materialized.Where(e => !instance.Any(oldElement => oldElement.SourceKey == e.SourceKey)))
                    {
                        added.ParentTabsId = mapping.GetValueOrDefault(templates.First(e => e.Id == added.SourceKey).ParentTabsId, "");
                        added.GroupPath = new(origin.GroupPath);
                        added.ZIndex = elements.Count == 0 ? 1 : elements.Max(e => e.ZIndex) + 1;
                        elements.Add(added);
                    }
                }
        }, false);
    }

    public void ResetComponentOverrides()
    {
        var key = SelectedInstanceKey;
        if (key.Length == 0)
            return;
        Edit("Правки экземпляра сброшены", _ =>
        {
            foreach (var current in CurrentLayer.Elements.Where(e => e.InstanceKey == key).ToList())
                if (Document.Components.FirstOrDefault(c => c.Id == current.ComponentKey)?.Elements.FirstOrDefault(e => e.Id == current.SourceKey)is { } source)
                    CurrentLayer.Elements[CurrentLayer.Elements.IndexOf(current)] = MergeTemplate(current, source, source, true);
        }, false);
    }

    public void DetachComponent()
    {
        var key = SelectedInstanceKey;
        if (key.Length == 0)
            return;
        Edit("Экземпляр отделён от основы", _ =>
        {
            foreach (var e in CurrentLayer.Elements.Where(e => e.InstanceKey == key))
                ComponentDefinition.Detach(e);
        });
    }

    private void TrackComponentTranslation(DraftDocument before)
    {
        foreach (var layer in Document.Layers)
            foreach (var instance in layer.Elements.Where(e => e.InstanceKey.Length > 0).GroupBy(e => e.InstanceKey))
            {
                var old = before.FindLayer(layer.Id)?.Elements.Where(e => e.InstanceKey == instance.Key).ToDictionary(e => e.Id);
                if (old is null || old.Count != instance.Count() || instance.Any(e => !old.ContainsKey(e.Id)))
                    continue;
                var deltas = instance.Select(e => (X: e.X - old[e.Id].X, Y: e.Y - old[e.Id].Y)).Distinct().ToList();
                if (deltas.Count != 1)
                    continue;
                var delta = deltas[0];
                foreach (var e in instance)
                {
                    e.ComponentOriginX = old[e.Id].ComponentOriginX + delta.X;
                    e.ComponentOriginY = old[e.Id].ComponentOriginY + delta.Y;
                }
            }
    }
}
