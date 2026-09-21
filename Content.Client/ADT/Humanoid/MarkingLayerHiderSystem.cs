using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Humanoid;

public sealed class MarkingLayerHiderSystem : EntitySystem
{
    [Dependency] private readonly MarkingManager _marking = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public void SetHiddenByOrgan(EntityUid body, EntityUid organ, List<Marking> applied)
    {
        var wanted = new HashSet<HumanoidVisualLayers>();
        foreach (var marking in applied)
        {
            if (!_marking.TryGetMarking(marking, out var proto) || proto.HidesLayers == null)
                continue;

            wanted.UnionWith(proto.HidesLayers);
        }

        var comp = CompOrNull<MarkingLayerHiderComponent>(body);

        if (comp == null)
        {
            if (wanted.Count == 0)
                return;

            comp = AddComp<MarkingLayerHiderComponent>(body);
        }

        var touched = new HashSet<HumanoidVisualLayers>(wanted);

        foreach (var (layer, organs) in comp.HiddenBy)
        {
            if (organs.Remove(organ))
                touched.Add(layer);
        }

        foreach (var layer in wanted)
        {
            if (!comp.HiddenBy.TryGetValue(layer, out var organs))
            {
                organs = new HashSet<EntityUid>();
                comp.HiddenBy[layer] = organs;
            }

            organs.Add(organ);
        }

        foreach (var layer in touched)
        {
            Refresh(body, comp, layer);
        }
    }

    private void Refresh(EntityUid body, MarkingLayerHiderComponent comp, HumanoidVisualLayers layer)
    {
        var hiddenByMarking = comp.HiddenBy.TryGetValue(layer, out var organs) && organs.Count > 0;

        if (!hiddenByMarking)
            comp.HiddenBy.Remove(layer);

        var hiddenByClothing = CompOrNull<HideableHumanoidLayersComponent>(body)?.HiddenLayers is { } clothingLayers
            && clothingLayers.ContainsKey(layer);

        var visible = !hiddenByMarking && !hiddenByClothing;

        if (_sprite.LayerMapTryGet(body, layer, out var index, false))
            _sprite.LayerSetVisible(body, index, visible);
    }
}
