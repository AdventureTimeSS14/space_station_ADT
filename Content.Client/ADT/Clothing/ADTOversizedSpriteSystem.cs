using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Clothing;

public sealed class ADTOversizedSpriteSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string DisplacementSuffix = "-displacement";
    private const string DisplacedShader = "DisplacedDraw";
    private const string DisplacedShaderUnshaded = "DisplacedDrawUnshaded";
    private const string UnshadedShader = "unshaded";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTOversizedSpriteComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTOversizedSpriteComponent, EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        SubscribeLocalEvent<ADTOversizedSpriteComponent, HeldVisualsUpdatedEvent>(OnHeldVisualsUpdated);
    }

    private void OnStartup(Entity<ADTOversizedSpriteComponent> ent, ref ComponentStartup args)
    {
        if (!ent.Comp.ScaleWorldSprite)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (GetScale(ent.Comp, sprite.BaseRSI?.Size.X) is not { } scale)
            return;

        _sprite.SetScale((ent.Owner, sprite), scale);
    }

    private void OnEquipmentVisualsUpdated(Entity<ADTOversizedSpriteComponent> ent, ref EquipmentVisualsUpdatedEvent args)
    {
        if (!ent.Comp.ScaleEquipped)
            return;

        ScaleRevealedLayers(ent, args.Equipee, args.RevealedLayers, ent.Comp.EquippedOffset);
    }

    private void OnHeldVisualsUpdated(Entity<ADTOversizedSpriteComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        if (!ent.Comp.ScaleInHand)
            return;

        ScaleRevealedLayers(ent, args.User, args.RevealedLayers, ent.Comp.InHandOffset);
    }

    private void ScaleRevealedLayers(
        Entity<ADTOversizedSpriteComponent> ent,
        EntityUid wearer,
        HashSet<string> revealedLayers,
        Vector2 offset)
    {
        if (!TryComp<SpriteComponent>(wearer, out var sprite))
            return;

        var target = new Entity<SpriteComponent?>(wearer, sprite);
        var scaled = new HashSet<string>();

        foreach (var key in revealedLayers)
        {
            if (key.EndsWith(DisplacementSuffix))
                continue;

            if (!_sprite.LayerMapTryGet(target, key, out var index, false))
                continue;

            var rsi = _sprite.LayerGetEffectiveRsi(target, index);

            if (GetScale(ent.Comp, rsi?.Size.X) is not { } scale)
                continue;

            if (!_sprite.TryGetLayer(target, index, out var layer, false))
                continue;

            _sprite.LayerSetScale(target, index, layer.Scale * scale);

            if (offset != Vector2.Zero)
                _sprite.LayerSetOffset(target, index, layer.Offset + offset);

            scaled.Add(key);
        }

        if (ent.Comp.UseDisplacement || scaled.Count == 0)
            return;

        foreach (var key in revealedLayers.ToArray())
        {
            if (!key.EndsWith(DisplacementSuffix))
                continue;

            if (!scaled.Contains(key[..^DisplacementSuffix.Length]))
                continue;

            DropDisplacement(target, sprite, key);
            revealedLayers.Remove(key);
        }
    }

    private void DropDisplacement(Entity<SpriteComponent?> target, SpriteComponent sprite, string displacementKey)
    {
        var ownerKey = displacementKey[..^DisplacementSuffix.Length];

        if (_sprite.LayerMapTryGet(target, ownerKey, out var ownerIndex, false)
            && _sprite.TryGetLayer(target, ownerIndex, out var ownerLayer, false))
        {
            switch (ownerLayer.ShaderPrototype?.Id)
            {
                case DisplacedShaderUnshaded:
                    sprite.LayerSetShader(ownerIndex, UnshadedShader);
                    break;
                case DisplacedShader:
                    sprite.LayerSetShader(ownerIndex, null, null);
                    break;
            }
        }

        _sprite.RemoveLayer(target, displacementKey, false);
    }

    private Vector2? GetScale(ADTOversizedSpriteComponent component, int? sourceSize)
    {
        if (component.Scale is { } manual)
            return manual;

        var source = component.SourceSize ?? sourceSize;

        if (source is not { } size || size <= 0 || size == component.CellSize)
            return null;

        var factor = component.CellSize / (float) size;

        return new Vector2(factor, factor);
    }
}
