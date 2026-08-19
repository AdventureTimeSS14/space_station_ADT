using Content.Shared.GhostTypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.GhostTypes;

public sealed class GhostBodyVisualsSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly MarkingManager _marking = default!;

    private static readonly string GhostVariantLayer = "ghostVariant";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostBodyAppearanceComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(Entity<GhostBodyAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyAppearance(ent);
    }

    private void ApplyAppearance(Entity<GhostBodyAppearanceComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var target = new Entity<SpriteComponent?>(ent, sprite);

        foreach (var (layer, data) in ent.Comp.Layers)
        {
            if (!_sprite.LayerMapTryGet(target, layer, out var index, false))
                continue;

            _sprite.LayerSetData(target, index, data);
        }

        ApplyMarkings(target, ent.Comp.Markings);

        if (_sprite.LayerMapTryGet(target, GhostVariantLayer, out var ghostIndex, false))
            _sprite.LayerSetVisible(target, ghostIndex, ent.Comp.Layers.Count == 0);
    }

    private void ApplyMarkings(Entity<SpriteComponent?> target, Dictionary<HumanoidVisualLayers, List<Marking>> markings)
    {
        foreach (var (bodyLayer, markingList) in markings)
        {
            if (!_sprite.LayerMapTryGet(target, bodyLayer, out var baseIndex, false))
                continue;

            foreach (var marking in markingList)
            {
                if (!_marking.TryGetMarking(marking, out var proto))
                    continue;

                for (var i = 0; i < proto.Sprites.Count; i++)
                {
                    var sprite = proto.Sprites[i];

                    if (sprite is not SpriteSpecifier.Rsi rsi)
                        continue;

                    var layerId = $"{proto.ID}-{rsi.RsiState}";

                    if (!_sprite.LayerMapTryGet(target, layerId, out _, false))
                    {
                        var layerData = new PrototypeLayerData
                        {
                            RsiPath = rsi.RsiPath.ToString(),
                            State = rsi.RsiState,
                            Shader = proto.Shader,
                        };
                        var newLayer = _sprite.AddLayer(target, layerData, baseIndex + i + 1);
                        _sprite.LayerMapSet(target, layerId, newLayer);
                    }

                    if (marking.MarkingColors is not null && i < marking.MarkingColors.Count)
                        _sprite.LayerSetColor(target, layerId, marking.MarkingColors[i]);
                    else
                        _sprite.LayerSetColor(target, layerId, Color.White);
                }
            }
        }
    }
}
