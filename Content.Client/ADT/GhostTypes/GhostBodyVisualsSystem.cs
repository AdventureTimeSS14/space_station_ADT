using Content.Shared.ADT.Ghost.GhostTypes;
using Content.Shared.Clothing;
using Content.Shared.DisplacementMap;
using Content.Shared.Ghost;
using Content.Shared.GhostTypes;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
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
        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnState(Entity<GhostBodyAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyAppearance(ent);
    }

    private void OnEquipmentVisualsUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Equipee))
            return;

        if (!TryComp<SpriteComponent>(args.Equipee, out var sprite))
            return;

        var displacement = GetDisplacement(args.Equipee, args.Slot);

        foreach (var key in args.RevealedLayers)
        {
            if (!_sprite.LayerMapTryGet((args.Equipee, sprite), key, out var index, false))
                continue;

            if (sprite[index] is not SpriteComponent.Layer layer)
                continue;

            if (layer.CopyToShaderParameters != null)
                continue;

            if (layer.ShaderPrototype == null)
            {
                sprite.LayerSetShader(index, SpriteSystem.UnshadedId.Id);
                continue;
            }

            if (displacement != null && layer.ShaderPrototype == displacement.ShaderOverride)
                sprite.LayerSetShader(index, displacement.ShaderOverrideUnshaded);
        }
    }

    private DisplacementData? GetDisplacement(EntityUid uid, string slot)
    {
        if (!TryComp<InventoryComponent>(uid, out var inventory))
            return null;

        var sex = CompOrNull<HumanoidProfileComponent>(uid)?.Sex;

        if (sex == Sex.Male && inventory.MaleDisplacements.Count > 0)
            return inventory.MaleDisplacements.GetValueOrDefault(slot);

        if (sex == Sex.Female && inventory.FemaleDisplacements.Count > 0)
            return inventory.FemaleDisplacements.GetValueOrDefault(slot);

        return inventory.Displacements.GetValueOrDefault(slot);
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
                            Shader = proto.Shader ?? SpriteSystem.UnshadedId.Id,
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
