using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.ADT.Cooking.Systems;
using Content.Shared.DisplacementMap;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Cooking;

public sealed partial class ADTCookingSystem : SharedADTCookingSystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private ADTSpriteCompositionSystem _composition = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<ShaderPrototype> ProgressShader = "ADTCookingProgress";
    private static readonly Color BurnColor = new(0.35f, 0.26f, 0.2f);

    private const int ModeFadeOut = 0;
    private const int ModeFadeIn = 1;
    private const int ModeBrown = 2;
    private const int ModeBurn = 3;

    private static readonly Vector2[] ToppingSpots =
    {
        new(-0.4f, 0.15f),
        new(0.35f, 0.2f),
        new(0.05f, -0.3f),
        new(0.4f, -0.25f),
        new(-0.35f, -0.3f),
        new(0f, 0.35f),
    };

    private readonly Dictionary<EntityUid, ShaderInstance[]> _shaders = new();
    private readonly Dictionary<string, EntityUid> _previews = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCookwareComponent, EntInsertedIntoContainerMessage>(OnCookwareInserted);
        SubscribeLocalEvent<ADTCookwareComponent, EntRemovedFromContainerMessage>(OnCookwareRemoved);
        SubscribeLocalEvent<ADTCookwareComponent, AppearanceChangeEvent>(OnCookwareAppearance);
        SubscribeLocalEvent<ADTCookwareComponent, AfterAutoHandleStateEvent>(OnCookwareState);
        SubscribeLocalEvent<ADTCookwareComponent, ComponentShutdown>(OnCookwareShutdown);

        SubscribeLocalEvent<ADTStoveComponent, EntInsertedIntoContainerMessage>(OnStoveInserted);
        SubscribeLocalEvent<ADTStoveComponent, EntRemovedFromContainerMessage>(OnStoveRemoved);
        SubscribeLocalEvent<ADTStoveComponent, AfterAutoHandleStateEvent>(OnStoveState);
        SubscribeLocalEvent<ADTStoveComponent, ComponentShutdown>(OnStoveShutdown);

        SubscribeLocalEvent<ADTPlateComponent, EntInsertedIntoContainerMessage>(OnPlateInserted);
        SubscribeLocalEvent<ADTPlateComponent, EntRemovedFromContainerMessage>(OnPlateRemoved);
        SubscribeLocalEvent<ADTPlateComponent, ComponentShutdown>(OnPlateShutdown);

        SubscribeLocalEvent<ADTDishComponent, AfterAutoHandleStateEvent>(OnDishState);
        SubscribeLocalEvent<ADTDishComponent, ComponentShutdown>(OnDishShutdown);

        InitializeParticles();
        InitializeFlip();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var preview in _previews.Values)
        {
            if (!TerminatingOrDeleted(preview))
                Del(preview);
        }

        _previews.Clear();
        _shaders.Clear();

        foreach (var uid in _emitters.Keys.ToList())
            RemoveParticles(uid);

        foreach (var uid in _ovenLoops.Keys.ToList())
            RemoveOvenLoop(uid);
    }

    private void OnPlateInserted(Entity<ADTPlateComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RefreshPlate(ent);
    }

    private void OnPlateRemoved(Entity<ADTPlateComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RefreshPlate(ent);
    }

    private void OnPlateShutdown(Entity<ADTPlateComponent> ent, ref ComponentShutdown args)
    {
        _composition.Forget(ent);
    }

    private void OnDishState(Entity<ADTDishComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp.Burn > 0f)
        {
            var color = Color.InterpolateBetween(Color.White, BurnColor, Math.Clamp(ent.Comp.Burn, 0f, 1f) * 0.75f);
            _sprite.SetColor((ent.Owner, sprite), color);
        }

        ApplySauce((ent.Owner, sprite));
        RefreshToppings(ent, sprite);

        if (!Container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        if (TryComp<ADTPlateComponent>(container.Owner, out var plate))
            RefreshPlate((container.Owner, plate));
        else if (TryComp<ADTCookwareComponent>(container.Owner, out var cookware))
            RefreshCookware((container.Owner, cookware));
    }

    private void OnDishShutdown(Entity<ADTDishComponent> ent, ref ComponentShutdown args)
    {
        _composition.Forget(ent);
    }

    private void RefreshToppings(Entity<ADTDishComponent> ent, SpriteComponent sprite)
    {
        var spriteEnt = new Entity<SpriteComponent>(ent, sprite);
        _composition.Clear(spriteEnt);

        if (ent.Comp.Toppings.Count == 0 || !_composition.TryGetVisualBounds(sprite, out var bounds))
            return;

        var half = bounds.Size / 2f;
        var size = MathF.Max(bounds.Width, bounds.Height) * 0.35f;
        for (var i = 0; i < ent.Comp.Toppings.Count; i++)
        {
            if (GetPreview(ent.Comp.Toppings[i]) is not { } preview || !_composition.TryGetVisualBounds(preview, out var toppingBounds))
                continue;

            var largest = MathF.Max(toppingBounds.Width, toppingBounds.Height);
            if (largest <= 0f)
                continue;

            var fit = MathF.Min(0.6f, size / largest);
            var scale = new Vector2(fit, fit);
            var spot = bounds.Center + ToppingSpots[i % ToppingSpots.Length] * half;
            _composition.CopyLayers(spriteEnt, preview, $"adt-dish-topping-{i}", spot - toppingBounds.Center * scale, scale, Angle.Zero, null);
        }
    }

    private void RefreshPlate(Entity<ADTPlateComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var spriteEnt = new Entity<SpriteComponent>(ent, sprite);
        _composition.Clear(spriteEnt);

        if (!_proto.TryIndex(ent.Comp.Layout, out var layoutProto))
            return;

        var items = ent.Comp.Contents.ContainedEntities;
        var layout = layoutProto.GetLayout(items.Count);
        if (layout == null || layout.Count == 0)
            return;

        var order = Enumerable.Range(0, Math.Min(items.Count, layout.Count))
            .OrderByDescending(i => layout[i].Offset.Y)
            .ToList();

        foreach (var i in order)
        {
            if (!TryComp<SpriteComponent>(items[i], out var itemSprite))
                continue;

            var slot = layout[i];
            var scale = new Vector2(slot.Scale, slot.Scale);
            var rotation = Angle.FromDegrees(slot.Rotation);

            if (layoutProto.ShadowColor.A > 0)
            {
                _composition.CopyLayers(spriteEnt, itemSprite, $"adt-plate-shadow-{i}", slot.Offset + layoutProto.ShadowOffset, scale, rotation, null, layoutProto.ShadowColor, slot.Displacement);
            }

            var sauce = CreateSauceShader(items[i], slot.Displacement != null);
            var displacement = slot.Displacement;
            if (sauce != null && displacement != null)
                displacement = new DisplacementData(displacement) { ShaderOverride = null };

            _composition.CopyLayers(spriteEnt, itemSprite, $"adt-plate-item-{i}", slot.Offset, scale, rotation, null, null, displacement, sauce);
        }
    }

    private void OnCookwareInserted(Entity<ADTCookwareComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RefreshCookware(ent);
    }

    private void OnCookwareRemoved(Entity<ADTCookwareComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RefreshCookware(ent);
    }

    private void OnCookwareAppearance(Entity<ADTCookwareComponent> ent, ref AppearanceChangeEvent args)
    {
        RefreshCookware(ent);
    }

    private void OnCookwareState(Entity<ADTCookwareComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshCookware(ent);
    }

    private void OnCookwareShutdown(Entity<ADTCookwareComponent> ent, ref ComponentShutdown args)
    {
        _composition.Forget(ent);
        _shaders.Remove(ent);
        _flips.Remove(ent);
        RemoveParticles(ent);
    }

    private void OnStoveInserted(Entity<ADTStoveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        RefreshStove(ent);

        if (TryComp<ADTCookwareComponent>(args.Entity, out var cookware))
            UpdateParticles((args.Entity, cookware));
    }

    private void OnStoveRemoved(Entity<ADTStoveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RefreshStove(ent);

        if (TryComp<ADTCookwareComponent>(args.Entity, out var cookware))
            UpdateParticles((args.Entity, cookware));
    }

    private void OnStoveState(Entity<ADTStoveComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshStove(ent);
    }

    private void OnStoveShutdown(Entity<ADTStoveComponent> ent, ref ComponentShutdown args)
    {
        _composition.Forget(ent);
        RemoveOvenLoop(ent);
    }

    private ShaderInstance[] GetShaders(Entity<ADTCookwareComponent> ent)
    {
        if (!_shaders.TryGetValue(ent, out var shaders))
        {
            var proto = _proto.Index(ProgressShader);
            shaders = new ShaderInstance[4];
            for (var mode = 0; mode < shaders.Length; mode++)
            {
                shaders[mode] = proto.InstanceUnique();
                shaders[mode].SetParameter("mode", (float) mode);
            }

            _shaders[ent] = shaders;
        }

        var comp = ent.Comp;
        var refTime = (float) (_timing.RealTime - (_timing.CurTime - comp.VisualRefTime)).TotalSeconds;
        foreach (var shader in shaders)
        {
            shader.SetParameter("refTime", refTime);
            shader.SetParameter("cookBase", comp.CookBase);
            shader.SetParameter("cookRate", comp.CookRate);
            shader.SetParameter("burnBase", comp.BurnBase);
            shader.SetParameter("burnRate", comp.BurnRate);
        }

        return shaders;
    }

    private SpriteComponent? GetPreview(EntProtoId proto)
    {
        if (!_previews.TryGetValue(proto.Id, out var uid) || TerminatingOrDeleted(uid))
        {
            if (!_proto.HasIndex<EntityPrototype>(proto))
                return null;

            uid = Spawn(proto, MapCoordinates.Nullspace);
            _previews[proto.Id] = uid;
        }

        return CompOrNull<SpriteComponent>(uid);
    }

    private void RefreshCookware(Entity<ADTCookwareComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var comp = ent.Comp;
        var spriteEnt = new Entity<SpriteComponent>(ent, sprite);
        _composition.Clear(spriteEnt);

        if (_sprite.LayerMapTryGet(spriteEnt.AsNullable(), ADTCookwareVisualLayers.Medium, out var mediumIndex, false))
        {
            _appearance.TryGetData<Color>(ent, ADTCookwareVisuals.MediumColor, out var color);
            _appearance.TryGetData<float>(ent, ADTCookwareVisuals.MediumFill, out var fill);
            _sprite.LayerSetVisible(spriteEnt.AsNullable(), mediumIndex, fill > 0.01f);
            _sprite.LayerSetColor(spriteEnt.AsNullable(), mediumIndex, color);

            if (comp.MediumState != null && comp.MediumLevels > 0 && fill > 0.01f)
            {
                var level = Math.Clamp((int) MathF.Ceiling(fill * comp.MediumLevels), 1, comp.MediumLevels);
                _sprite.LayerSetRsiState(spriteEnt.AsNullable(), mediumIndex, $"{comp.MediumState}-{level}");
            }
        }

        UpdateParticles(ent);

        int? insertAt = null;

        var shaders = GetShaders(ent);
        var cooking = comp.Stage == ADTCookingStage.Cooking;
        var finished = comp.Stage is ADTCookingStage.Done or ADTCookingStage.Overcooked;

        if (_sprite.LayerMapTryGet(spriteEnt.AsNullable(), ADTCookwareVisualLayers.Batter, out var batterIndex, false))
        {
            _appearance.TryGetData<bool>(ent, ADTCookwareVisuals.BatterVisible, out var batterVisible);
            _appearance.TryGetData<Color>(ent, ADTCookwareVisuals.BatterColor, out var batterColor);
            _sprite.LayerSetVisible(spriteEnt.AsNullable(), batterIndex, batterVisible);
            _sprite.LayerSetColor(spriteEnt.AsNullable(), batterIndex, batterColor.WithAlpha(1f));

            ShaderInstance? batterShader = null;
            if (cooking)
                batterShader = comp.CookCenterResult != null ? shaders[ModeFadeOut] : shaders[ModeBrown];

            sprite.LayerSetShader(batterIndex, batterShader);
        }

        var contents = comp.Contents.ContainedEntities;
        var large = new List<EntityUid>();
        var small = new List<EntityUid>();
        foreach (var item in contents)
        {
            if (IsLargeItem(comp, item))
                large.Add(item);
            else
                small.Add(item);
        }

        var drawn = 0;
        for (var i = 0; i < large.Count + small.Count; i++)
        {
            var isLarge = i < large.Count;
            var item = isLarge ? large[i] : small[i - large.Count];
            if (!TryComp<SpriteComponent>(item, out var itemSprite))
                continue;

            Vector2 position;
            if (isLarge || small.Count == 1 || comp.ContentOffsets.Count == 0)
                position = comp.ContentCenter;
            else
                position = comp.ContentOffsets[(i - large.Count) % comp.ContentOffsets.Count];

            var scale = isLarge ? GetFitScale(comp, itemSprite) : comp.ContentScale;
            ApplyFlip(ent, ref position, ref scale);
            var key = drawn++;

            if (cooking && comp.CookResults.TryGetValue(GetNetEntity(item), out var result) && GetPreview(result) is { } preview)
            {
                insertAt = _composition.CopyLayers(spriteEnt, itemSprite, $"adt-cookware-item-{key}", GetCenteredOffset(itemSprite, position, scale), scale, Angle.Zero, insertAt, shader: shaders[ModeFadeOut]);
                insertAt = _composition.CopyLayers(spriteEnt, preview, $"adt-cookware-result-{key}", GetCenteredOffset(preview, position, scale), scale, Angle.Zero, insertAt, shader: shaders[ModeFadeIn]);
                continue;
            }

            ShaderInstance? shader = null;
            if (cooking)
                shader = comp.CookCenterResult != null ? shaders[ModeFadeOut] : shaders[ModeBrown];
            else if (finished)
                shader = shaders[ModeBurn];

            insertAt = _composition.CopyLayers(spriteEnt, itemSprite, $"adt-cookware-item-{key}", GetCenteredOffset(itemSprite, position, scale), scale, Angle.Zero, insertAt, shader: shader);
        }

        if (cooking && comp.CookCenterResult is { } center && GetPreview(center) is { } centerPreview)
        {
            var scale = comp.CookCenterLarge ? GetFitScale(comp, centerPreview) : comp.ContentScale;
            var position = comp.ContentCenter;
            ApplyFlip(ent, ref position, ref scale);
            _composition.CopyLayers(spriteEnt, centerPreview, "adt-cookware-center", GetCenteredOffset(centerPreview, position, scale), scale, Angle.Zero, insertAt, shader: shaders[ModeFadeIn]);
        }

        if (TryGetStoveSlot(ent, out var stove, out _))
            RefreshStove(stove);
    }

    private bool IsLargeItem(ADTCookwareComponent comp, EntityUid item)
    {
        if (comp.LargeItems.Contains(GetNetEntity(item)))
            return true;

        var proto = MetaData(item).EntityPrototype?.ID;
        return proto != null && Settings.BaseIngredients.Contains(proto);
    }

    private Vector2 GetFitScale(ADTCookwareComponent comp, SpriteComponent sprite)
    {
        if (!_composition.TryGetVisualBounds(sprite, out var bounds) || bounds.Width <= 0f || bounds.Height <= 0f)
            return comp.ContentScale;

        var fit = MathF.Min(comp.ContentArea.X / bounds.Width, comp.ContentArea.Y / bounds.Height);
        return new Vector2(fit, fit);
    }

    private Vector2 GetCenteredOffset(SpriteComponent sprite, Vector2 position, Vector2 scale)
    {
        if (!_composition.TryGetVisualBounds(sprite, out var bounds))
            return position;

        return position - bounds.Center * scale;
    }

    private void RefreshStove(Entity<ADTStoveComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var spriteEnt = new Entity<SpriteComponent>(ent, sprite);
        _composition.Clear(spriteEnt);
        UpdateOvenLoop(ent);

        var order = Enumerable.Range(0, ent.Comp.Slots.Count)
            .OrderByDescending(i => ent.Comp.Slots[i].Offset.Y)
            .ToList();

        foreach (var i in order)
        {
            var slot = ent.Comp.Slots[i];

            if (slot.Heat != ADTStoveHeat.Off)
            {
                var state = slot.Type == ADTStoveSlotType.Oven
                    ? ent.Comp.OvenState
                    : (slot.FlameState ?? ent.Comp.BurnerFlameState) + slot.Heat.ToString().ToLowerInvariant();

                var flameOffset = slot.Type == ADTStoveSlotType.Oven ? Vector2.Zero : slot.Offset;
                _composition.AddLayer(spriteEnt, $"adt-stove-flame-{i}", new SpriteSpecifier.Rsi(new ResPath(ent.Comp.FlameRsi), state), flameOffset);
            }

            var cookware = GetSlotCookware(ent, i);
            if (cookware == null || !TryComp<SpriteComponent>(cookware, out var cookwareSprite))
                continue;

            var offset = slot.Offset;
            var scale = slot.Scale ?? ent.Comp.CookwareScale;
            if (slot.Scale == null && TryComp<ADTCookwareComponent>(cookware, out var cookwareComp))
            {
                scale = cookwareComp.StoveScale ?? scale;
                offset += cookwareComp.StoveOffset;
            }

            if (slot.Mirror)
            {
                scale.X = -scale.X;
                offset.X = slot.Offset.X - (offset.X - slot.Offset.X);
            }

            _composition.CopyLayers(spriteEnt, cookwareSprite, $"adt-stove-cookware-{i}", offset, scale, Angle.Zero, null, slot.Tint);
        }
    }
}
