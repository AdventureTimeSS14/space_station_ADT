using System.Linq;
using System.Numerics;
using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.Systems;
using Content.Client._RMC14.Attachable.Components;
using Content.Client._RMC14.Attachable.Systems;
using Content.Shared._RMC14.Attachable.Events;
using Content.Shared.ADT.Weapons.Ranged.Components;
using Content.Shared.ADT.Weapons.Ranged.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Weapons.Ranged;

public sealed partial class GunHeatSystem : SharedGunHeatSystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly ProtoId<ShaderPrototype> _heatShader = "GunHeat";
    private const string BarrelSlot = "rmc-aslot-barrel";
    private static readonly ProtoId<ShaderPrototype> _inhandHeatShader = "GunHeatInhand";
    private static readonly ProtoId<ShaderPrototype> _inhandHeatDisplacedShader = "GunHeatInhandDisplaced";
    private static readonly ProtoId<ShaderPrototype> _inhandHeatDisplacedStencilShader = "GunHeatInhandDisplacedStencil";

    private const string DisplacedDrawShader = "DisplacedDraw";
    private const string DisplacedStencilDrawShader = "DisplacedStencilDraw";
    private const string DisplacementSuffix = "-displacement";

    private readonly Dictionary<EntityUid, (ShaderInstance Instance, ShaderInstance? Shader, string? Prototype)> _glowing = new();
    private readonly Dictionary<EntityUid, (ShaderInstance Instance, ShaderInstance? Shader, string? Prototype)> _barrelGlow = new();

    private readonly Dictionary<EntityUid, HeldGlow> _held = new();

    private sealed class HeldGlow(EntityUid holder, string[] layers)
    {
        public readonly EntityUid Holder = holder;
        public readonly string[] Layers = layers;

        public readonly Dictionary<string, (ShaderInstance Instance, ShaderInstance? Shader, string? Prototype)> Glowing = new();
    }

    private readonly Dictionary<EntityUid, GunHeatStatusControl> _statusControls = new();

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<GunHeatComponent> ent, ref ComponentShutdown args)
    {
        _glowing.Remove(ent);
        _barrelGlow.Remove(ent);
        _held.Remove(ent);
        _statusControls.Remove(ent);
    }

    [SubscribeLocalEvent]
    private void OnHandleState(Entity<GunHeatComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var glow = GetGlow(ent.Comp, ent.Comp.Temperature);
        UpdateItemGlow(ent, glow);
        UpdateInhandGlow(ent, glow);

        if (_statusControls.TryGetValue(ent, out var control))
            control.Update(ent.Comp);
    }

    [SubscribeLocalEvent(after: [typeof(GunSystem)])]
    private void OnItemStatus(Entity<GunHeatComponent> ent, ref ItemStatusCollectMessage args)
    {
        var control = new GunHeatStatusControl();
        control.Update(ent.Comp);
        _statusControls[ent] = control;
        args.Controls.Add(control);
    }

    [SubscribeLocalEvent]
    private void OnHeldVisualsUpdated(Entity<GunHeatComponent> ent, ref HeldVisualsUpdatedEvent args)
    {
        if (args.RevealedLayers.Count == 0)
        {
            if (!_hands.IsHolding(args.User, ent))
                _held.Remove(ent);

            return;
        }

        var layers = args.RevealedLayers.Where(key => !key.EndsWith(DisplacementSuffix)).ToArray();
        _held[ent] = new HeldGlow(args.User, layers);
        UpdateInhandGlow(ent, GetGlow(ent.Comp, ent.Comp.Temperature));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var held in _held.Values)
        {
            if (held.Glowing.Count == 0)
                continue;

            var direction = GetMuzzleDirection(held.Holder);
            foreach (var (instance, _, _) in held.Glowing.Values)
            {
                instance.SetParameter("muzzleDir", direction);
            }
        }

        // The barrel module is a layer added after the gun sprite. Reapply once it exists,
        // otherwise it draws on top of the glow and stays cold metal.
        List<EntityUid>? pendingBarrel = null;
        foreach (var uid in _glowing.Keys)
        {
            if (_barrelGlow.ContainsKey(uid))
                continue;

            if (!TryComp<SpriteComponent>(uid, out var sprite) || !sprite.LayerMapTryGet(BarrelSlot, out _))
                continue;

            pendingBarrel ??= new List<EntityUid>();
            pendingBarrel.Add(uid);
        }

        if (pendingBarrel == null)
            return;

        foreach (var uid in pendingBarrel)
        {
            if (!TryComp<GunHeatComponent>(uid, out var heat))
                continue;

            UpdateItemGlow((uid, heat), GetGlow(heat, heat.Temperature));
        }
    }

    private void UpdateItemGlow(Entity<GunHeatComponent> ent, float glow)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent.Owner, sprite), GunVisualLayers.Base, out var layerIndex, false))
            layerIndex = 0;

        if (layerIndex >= sprite.AllLayers.Count() || sprite[layerIndex] is not SpriteComponent.Layer layer)
        {
            UpdateBarrelGlow(ent, sprite, glow, null, default);
            return;
        }

        if (glow <= 0f)
        {
            if (_glowing.Remove(ent, out var saved))
                sprite.LayerSetShader(layerIndex, saved.Shader, saved.Prototype);

            UpdateBarrelGlow(ent, sprite, glow, null, default);
            return;
        }

        if (!_glowing.TryGetValue(ent, out var state))
        {
            if (!_prototype.TryIndex(_heatShader, out var shader))
            {
                UpdateBarrelGlow(ent, sprite, glow, null, default);
                return;
            }

            state = (shader.InstanceUnique(), layer.Shader, layer.ShaderPrototype?.Id);
            _glowing[ent] = state;
            sprite.LayerSetShader(layerIndex, state.Instance, _heatShader);
        }

        state.Instance.SetParameter("heat", glow);
        var gunTip = LayerTip(layer, ent.Comp.MuzzleFraction);
        var outerTip = gunTip;
        if (TryGetBarrelLayer(sprite, layerIndex, out var barrel))
            outerTip = Math.Max(outerTip, LayerTip(barrel, BarrelFraction(ent.Owner)));

        var gradient = MakeGradient(ent.Comp, glow, outerTip - gunTip);
        ApplyHeatGradient(state.Instance, glow, layer, outerTip, gradient);
        UpdateBarrelGlow(ent, sprite, glow, outerTip, gradient);
    }

    [SubscribeLocalEvent(after: [typeof(AttachableHolderVisualsSystem)])]
    private void OnAttachablesAltered(Entity<GunHeatComponent> ent, ref AttachableHolderAttachablesAlteredEvent args)
    {
        if (args.SlotId != BarrelSlot)
            return;

        // The overlay was just rebuilt, so the saved shader no longer belongs to that layer.
        _barrelGlow.Remove(ent.Owner);
        UpdateItemGlow(ent, GetGlow(ent.Comp, ent.Comp.Temperature));
    }

    private readonly record struct HeatGradient(float HotEnd, float FadeEnd);

    private void UpdateBarrelGlow(Entity<GunHeatComponent> ent, SpriteComponent sprite, float glow, float? outerTip, HeatGradient gradient)
    {
        if (!TryGetBarrelLayer(sprite, -1, out var layer))
        {
            _barrelGlow.Remove(ent.Owner);
            return;
        }

        if (!_sprite.LayerMapTryGet((ent.Owner, sprite), BarrelSlot, out var layerIndex, false))
        {
            _barrelGlow.Remove(ent.Owner);
            return;
        }

        if (glow <= 0f)
        {
            if (_barrelGlow.Remove(ent.Owner, out var saved))
                sprite.LayerSetShader(layerIndex, saved.Shader, saved.Prototype);
            return;
        }

        var tip = Math.Max(outerTip ?? float.NegativeInfinity, LayerTip(layer, BarrelFraction(ent.Owner)));
        if (outerTip == null)
            gradient = MakeGradient(ent.Comp, glow, 0f);

        if (!_barrelGlow.TryGetValue(ent.Owner, out var state))
        {
            if (!_prototype.TryIndex(_heatShader, out var shader))
                return;

            state = (shader.InstanceUnique(), layer.Shader, layer.ShaderPrototype?.Id);
            _barrelGlow[ent.Owner] = state;
            sprite.LayerSetShader(layerIndex, state.Instance, _heatShader);
        }

        ApplyHeatGradient(state.Instance, glow, layer, tip, gradient);
    }

    private bool TryGetBarrelLayer(SpriteComponent sprite, int ignoreIndex, out SpriteComponent.Layer layer)
    {
        layer = null!;
        if (!_sprite.LayerMapTryGet((sprite.Owner, sprite), BarrelSlot, out var barrelIndex, false)
            || barrelIndex == ignoreIndex
            || barrelIndex >= sprite.AllLayers.Count()
            || sprite[barrelIndex] is not SpriteComponent.Layer found)
        {
            return false;
        }

        layer = found;
        return true;
    }

    private static HeatGradient MakeGradient(GunHeatComponent comp, float heat, float extension)
    {
        // extension is how far a barrel module sticks past the gun's own muzzle.
        // Add it to the fade so the tube stays in the same gradient instead of starting a new one.
        var hotEnd = comp.GlowReachStart + (comp.GlowReachFull - comp.GlowReachStart) * heat + extension * heat;
        var fadeEnd = comp.GlowFadeStart + (comp.GlowFadeFull - comp.GlowFadeStart) * heat + extension;
        if (fadeEnd < hotEnd + comp.GlowFadeGap)
            fadeEnd = hotEnd + comp.GlowFadeGap;

        return new HeatGradient(hotEnd, fadeEnd);
    }

    private void ApplyHeatGradient(ShaderInstance instance, float glow, SpriteComponent.Layer layer, float tipX, HeatGradient gradient)
    {
        var (origin, span) = LayerSpan(layer);
        instance.SetParameter("heat", glow);
        instance.SetParameter("tipX", tipX);
        instance.SetParameter("originX", origin);
        instance.SetParameter("spanX", span);
        instance.SetParameter("hotEnd", gradient.HotEnd);
        instance.SetParameter("fadeEnd", gradient.FadeEnd);
    }

    private static (float Origin, float Span) LayerSpan(SpriteComponent.Layer layer)
    {
        var ppm = (float) EyeManager.PixelsPerMeter;
        var span = Math.Max(layer.PixelSize.X, 1) / ppm * Math.Max(layer.Scale.X, 0.001f);
        return (layer.Offset.X - span * 0.5f, span);
    }

    private float LayerTip(SpriteComponent.Layer layer, float fraction)
    {
        var (origin, span) = LayerSpan(layer);
        return origin + Math.Clamp(fraction, 0.01f, 1f) * span;
    }

    /// <summary>
    /// Barrel modules are painted on the left of a full tile. The fraction says where the metal actually ends.
    /// </summary>
    private float BarrelFraction(EntityUid gun)
    {
        if (!_containers.TryGetContainer(gun, BarrelSlot, out var container))
            return 1f;

        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<AttachableVisualsComponent>(contained, out var visuals) && visuals.MuzzleFraction > 0f)
                return visuals.MuzzleFraction;
        }

        return 1f;
    }

    private void UpdateInhandGlow(Entity<GunHeatComponent> ent, float glow)
    {
        if (!_held.TryGetValue(ent, out var held) || !TryComp<SpriteComponent>(held.Holder, out var sprite))
            return;

        var direction = GetMuzzleDirection(held.Holder);

        foreach (var key in held.Layers)
        {
            if (!_sprite.LayerMapTryGet((held.Holder, sprite), key, out var index, false)
                || sprite[index] is not SpriteComponent.Layer layer)
                continue;

            if (glow <= 0f)
            {
                if (held.Glowing.Remove(key, out var original))
                    sprite.LayerSetShader(index, original.Shader, original.Prototype);
                continue;
            }

            if (!held.Glowing.TryGetValue(key, out var state))
            {
                if (GetInhandShader(layer) is not { } shader)
                    continue;

                if (!_prototype.TryIndex(shader, out var heatShader))
                    continue;

                state = (heatShader.InstanceUnique(), layer.Shader, layer.ShaderPrototype?.Id);
                held.Glowing[key] = state;
                sprite.LayerSetShader(index, state.Instance, shader);
            }

            state.Instance.SetParameter("heat", glow);
            state.Instance.SetParameter("muzzleDir", direction);
            state.Instance.SetParameter("reachMin", ent.Comp.InhandReachStart);
            state.Instance.SetParameter("reachMax", ent.Comp.InhandReachFull);
        }
    }

    private static ProtoId<ShaderPrototype>? GetInhandShader(SpriteComponent.Layer layer)
        => layer.ShaderPrototype?.Id switch
        {
            null when layer.Shader == null => _inhandHeatShader,
            DisplacedDrawShader => _inhandHeatDisplacedShader,
            DisplacedStencilDrawShader => _inhandHeatDisplacedStencilShader,
            _ => (ProtoId<ShaderPrototype>?) null,
        };

    private Vector2 GetMuzzleDirection(EntityUid holder)
    {
        var angle = (_transform.GetWorldRotation(holder) + _eye.CurrentEye.Rotation).Reduced().FlipPositive();

        return SpriteComponent.Layer.GetDirection(RsiDirectionType.Dir4, angle) switch
        {
            RsiDirection.North => new Vector2(0f, 1f),
            RsiDirection.East => new Vector2(1f, 0f),
            RsiDirection.West => new Vector2(-1f, 0f),
            _ => new Vector2(0f, -1f),
        };
    }
}
