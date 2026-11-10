using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.ADT.Cooking.Systems;
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
    private readonly HashSet<EntProtoId> _queuedPreviews = new();
    private readonly HashSet<EntityUid> _previewWaiters = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCookwareComponent, EntInsertedIntoContainerMessage>(OnCookwareInserted);
        SubscribeLocalEvent<ADTCookwareComponent, EntRemovedFromContainerMessage>(OnCookwareRemoved);
        SubscribeLocalEvent<ADTCookwareComponent, AppearanceChangeEvent>(OnCookwareAppearance);
        SubscribeLocalEvent<ADTCookwareComponent, AfterAutoHandleStateEvent>(OnCookwareState);
        SubscribeLocalEvent<ADTCookwareComponent, ComponentStartup>(OnCookwareStartup);
        SubscribeLocalEvent<ADTCookwareComponent, ComponentShutdown>(OnCookwareShutdown);

        SubscribeLocalEvent<ADTStoveComponent, EntInsertedIntoContainerMessage>(OnStoveInserted);
        SubscribeLocalEvent<ADTStoveComponent, EntRemovedFromContainerMessage>(OnStoveRemoved);
        SubscribeLocalEvent<ADTStoveComponent, AfterAutoHandleStateEvent>(OnStoveState);
        SubscribeLocalEvent<ADTStoveComponent, ComponentShutdown>(OnStoveShutdown);

        SubscribeLocalEvent<ADTPlateComponent, EntInsertedIntoContainerMessage>(OnPlateInserted);
        SubscribeLocalEvent<ADTPlateComponent, EntRemovedFromContainerMessage>(OnPlateRemoved);
        SubscribeLocalEvent<ADTPlateComponent, ComponentStartup>(OnPlateStartup);
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
        _queuedPreviews.Clear();
        _previewWaiters.Clear();
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

    private void OnPlateStartup(Entity<ADTPlateComponent> ent, ref ComponentStartup args)
    {
        RefreshPlate(ent);
    }

    private void OnPlateShutdown(Entity<ADTPlateComponent> ent, ref ComponentShutdown args)
    {
        _composition.Forget(ent);
    }

    private void OnDishState(Entity<ADTDishComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshDish(ent);
    }

    private void RefreshDish(Entity<ADTDishComponent> ent)
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
            if (GetPreview(ent.Comp.Toppings[i], ent) is not { } preview || !_composition.TryGetVisualBounds(preview, out var toppingBounds))
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
        if (!ent.Comp.Initialized || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var spriteEnt = new Entity<SpriteComponent>(ent, sprite);
        _composition.Clear(spriteEnt);

        if (!_proto.TryIndex(ent.Comp.Layout, out var layoutProto))
            return;

        if (layoutProto.Arrangements.Count == 0)
            return;

        var entries = new List<PlateEntry>();
        var maxRowWidth = layoutProto.Arrangements.Max(a => a.Max(r => r.Width));

        foreach (var item in ent.Comp.Contents.ContainedEntities)
        {
            if (!TryComp<SpriteComponent>(item, out var itemSprite))
                continue;

            var width = 16f;
            var height = 16f;
            var anchor = Vector2.Zero;

            if (_composition.TryGetVisualBounds(itemSprite, out var bounds) && bounds.Width > 0f && bounds.Height > 0f)
            {
                width = bounds.Width * 32f;
                height = bounds.Height * 32f;
                anchor = new Vector2(bounds.Center.X, bounds.Bottom);
            }

            var scale = MathF.Min(layoutProto.MaxScale, MathF.Min(layoutProto.MaxHeight / height, maxRowWidth / width));
            entries.Add(new PlateEntry(item, itemSprite, anchor, width, scale));
        }

        if (entries.Count == 0)
            return;

        var bySize = entries.OrderByDescending(e => e.Width * e.Scale).ToList();

        List<PlateEntry>[]? bestRows = null;
        List<ADTPlateRow>? bestArrangement = null;
        var bestFit = -1f;

        foreach (var arrangement in layoutProto.Arrangements)
        {
            if (arrangement.Count == 0)
                continue;

            var rows = new List<PlateEntry>[arrangement.Count];
            for (var r = 0; r < rows.Length; r++)
            {
                rows[r] = new List<PlateEntry>();
            }

            foreach (var entry in bySize)
            {
                var target = 0;
                var targetLoad = float.MaxValue;
                for (var r = 0; r < rows.Length; r++)
                {
                    rows[r].Add(entry);
                    var load = GetRowSpan(rows[r], layoutProto.Spacing) / arrangement[r].Width;
                    rows[r].RemoveAt(rows[r].Count - 1);

                    if (load < targetLoad)
                    {
                        target = r;
                        targetLoad = load;
                    }
                }

                rows[target].Add(entry);
            }

            var fit = 1f;
            for (var r = 0; r < rows.Length; r++)
            {
                var span = GetRowSpan(rows[r], layoutProto.Spacing);
                if (span > 0f)
                    fit = MathF.Min(fit, arrangement[r].Width / span);
            }

            if (fit > bestFit)
            {
                bestFit = fit;
                bestRows = rows;
                bestArrangement = arrangement;
            }
        }

        if (bestRows == null || bestArrangement == null)
            return;

        var order = Enumerable.Range(0, bestRows.Length)
            .OrderByDescending(r => bestArrangement[r].Offset.Y);

        var key = 0;
        foreach (var r in order)
        {
            var row = bestRows[r];
            if (row.Count == 0)
                continue;

            var cursor = -GetRowSpan(row, layoutProto.Spacing) * bestFit / 2f + row[0].Width * row[0].Scale * bestFit / 2f;

            for (var j = 0; j < row.Count; j++)
            {
                var entry = row[j];
                if (j > 0)
                    cursor += (row[j - 1].Width * row[j - 1].Scale + entry.Width * entry.Scale) / 2f * bestFit * layoutProto.Spacing;

                var fit = MathF.Max(layoutProto.MinScale, entry.Scale * bestFit);
                var scale = new Vector2(fit, fit);
                var offset = bestArrangement[r].Offset + new Vector2(cursor / 32f, 0f) - entry.Anchor * scale;

                if (layoutProto.ShadowColor.A > 0)
                {
                    _composition.CopyLayers(spriteEnt, entry.Sprite, $"adt-plate-shadow-{key}", offset + layoutProto.ShadowOffset, scale, Angle.Zero, null, fill: layoutProto.ShadowColor);
                }

                var sauce = CreateSauceShader(entry.Uid);
                _composition.CopyLayers(spriteEnt, entry.Sprite, $"adt-plate-item-{key}", offset, scale, Angle.Zero, null, shader: sauce, keepOwnShaders: true, replaceOwnShader: SauceShader);
                key++;
            }
        }
    }

    private static float GetRowSpan(List<PlateEntry> row, float spacing)
    {
        if (row.Count == 0)
            return 0f;

        var span = (row[0].Width * row[0].Scale + row[row.Count - 1].Width * row[row.Count - 1].Scale) / 2f;
        for (var j = 1; j < row.Count; j++)
        {
            span += (row[j - 1].Width * row[j - 1].Scale + row[j].Width * row[j].Scale) / 2f * spacing;
        }

        return span;
    }

    private readonly struct PlateEntry
    {
        public readonly EntityUid Uid;
        public readonly SpriteComponent Sprite;
        public readonly Vector2 Anchor;
        public readonly float Width;
        public readonly float Scale;

        public PlateEntry(EntityUid uid, SpriteComponent sprite, Vector2 anchor, float width, float scale)
        {
            Uid = uid;
            Sprite = sprite;
            Anchor = anchor;
            Width = width;
            Scale = scale;
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

    private void OnCookwareStartup(Entity<ADTCookwareComponent> ent, ref ComponentStartup args)
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

    private SpriteComponent? GetPreview(EntProtoId proto, EntityUid requester)
    {
        if (_previews.TryGetValue(proto.Id, out var uid) && !TerminatingOrDeleted(uid))
            return CompOrNull<SpriteComponent>(uid);

        if (!_proto.HasIndex<EntityPrototype>(proto))
            return null;

        _queuedPreviews.Add(proto);
        _previewWaiters.Add(requester);
        return null;
    }

    private void SpawnQueuedPreviews()
    {
        if (_queuedPreviews.Count == 0)
            return;

        foreach (var proto in _queuedPreviews)
        {
            if (_previews.TryGetValue(proto.Id, out var existing) && !TerminatingOrDeleted(existing))
                continue;

            var uid = Spawn(proto, MapCoordinates.Nullspace);
            _previews[proto.Id] = uid;
            _appearance.OnChangeData(uid, CompOrNull<SpriteComponent>(uid));
        }

        _queuedPreviews.Clear();

        var waiters = _previewWaiters.ToList();
        _previewWaiters.Clear();
        foreach (var waiter in waiters)
        {
            if (TryComp<ADTCookwareComponent>(waiter, out var cookware))
                RefreshCookware((waiter, cookware));
            else if (TryComp<ADTDishComponent>(waiter, out var dish))
                RefreshDish((waiter, dish));
        }
    }

    private void RefreshCookware(Entity<ADTCookwareComponent> ent)
    {
        if (!ent.Comp.Initialized || !TryComp<SpriteComponent>(ent, out var sprite))
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

            var batterPosition = comp.ContentCenter;
            var batterScale = Vector2.One;
            ApplyFlip(ent, ref batterPosition, ref batterScale);
            _sprite.LayerSetScale(spriteEnt.AsNullable(), batterIndex, batterScale);
            _sprite.LayerSetOffset(spriteEnt.AsNullable(), batterIndex, batterPosition - comp.ContentCenter * batterScale);
        }

        var contents = comp.Contents.ContainedEntities;
        var toppings = new List<EntityUid>();
        if (cooking && comp.CookCenterResult != null)
        {
            foreach (var netTopping in comp.CookToppings)
            {
                if (TryGetEntity(netTopping, out var topping) && contents.Contains(topping.Value))
                    toppings.Add(topping.Value);
            }
        }

        var large = new List<EntityUid>();
        var small = new List<EntityUid>();
        foreach (var item in contents)
        {
            if (toppings.Contains(item))
                continue;

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

            if (cooking && comp.CookResults.TryGetValue(GetNetEntity(item), out var result) && GetPreview(result, ent) is { } preview)
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

        if (cooking && comp.CookCenterResult is { } center && GetPreview(center, ent) is { } centerPreview)
        {
            var scale = comp.CookCenterLarge ? GetFitScale(comp, centerPreview) : comp.ContentScale;
            var position = comp.ContentCenter;
            ApplyFlip(ent, ref position, ref scale);
            insertAt = _composition.CopyLayers(spriteEnt, centerPreview, "adt-cookware-center", GetCenteredOffset(centerPreview, position, scale), scale, Angle.Zero, insertAt, shader: shaders[ModeFadeIn]);

            var toppingShader = comp.CookToppingsStay ? null : shaders[ModeFadeOut];
            DrawCookToppings(spriteEnt, centerPreview, toppings, position, scale, insertAt, toppingShader);
        }

        if (TryGetStoveSlot(ent, out var stove, out _))
            RefreshStove(stove);
    }

    private void DrawCookToppings(Entity<SpriteComponent> target, SpriteComponent center, List<EntityUid> toppings, Vector2 position, Vector2 scale, int? insertAt, ShaderInstance? shader)
    {
        if (toppings.Count == 0 || !_composition.TryGetVisualBounds(center, out var bounds))
            return;

        var origin = GetCenteredOffset(center, position, scale);
        var half = bounds.Size / 2f;
        var size = MathF.Max(bounds.Width, bounds.Height) * 0.35f;
        for (var i = 0; i < toppings.Count; i++)
        {
            if (!TryComp<SpriteComponent>(toppings[i], out var toppingSprite) || !_composition.TryGetVisualBounds(toppingSprite, out var toppingBounds))
                continue;

            var largest = MathF.Max(toppingBounds.Width, toppingBounds.Height);
            if (largest <= 0f)
                continue;

            var fit = MathF.Min(0.6f, size / largest);
            var spot = bounds.Center + ToppingSpots[i % ToppingSpots.Length] * half;
            var local = spot - toppingBounds.Center * fit;
            insertAt = _composition.CopyLayers(target, toppingSprite, $"adt-cookware-topping-{i}", origin + local * scale, scale * fit, Angle.Zero, insertAt, shader: shader);
        }
    }

    private void DrawOvenView(Entity<SpriteComponent> stove, int index, ADTStoveSlot slot, Entity<ADTCookwareComponent> cookware, ResPath rsi)
    {
        var comp = cookware.Comp;
        if (!comp.Initialized)
            return;

        var tint = slot.Tint ?? Color.White;

        var trayIndex = _composition.AddLayer(stove, $"adt-stove-oven-{index}", new SpriteSpecifier.Rsi(rsi, comp.OvenState!), slot.Offset);
        _sprite.LayerSetColor(stove.AsNullable(), trayIndex, tint);

        _appearance.TryGetData<bool>(cookware, ADTCookwareVisuals.BatterVisible, out var batterVisible);
        if (batterVisible && comp.OvenBatterState != null)
        {
            _appearance.TryGetData<Color>(cookware, ADTCookwareVisuals.BatterColor, out var batterColor);
            var batterIndex = _composition.AddLayer(stove, $"adt-stove-oven-batter-{index}", new SpriteSpecifier.Rsi(rsi, comp.OvenBatterState), slot.Offset);
            _sprite.LayerSetColor(stove.AsNullable(), batterIndex, batterColor.WithAlpha(1f) * tint);
        }

        var items = new List<(EntityUid Uid, SpriteComponent Sprite, bool Large)>();
        foreach (var item in comp.Contents.ContainedEntities)
        {
            if (TryComp<SpriteComponent>(item, out var itemSprite))
                items.Add((item, itemSprite, IsLargeItem(comp, item)));
        }

        var area = comp.OvenContentArea;
        var smallCount = items.Count(entry => !entry.Large);
        var smallIndex = 0;
        for (var i = 0; i < items.Count; i++)
        {
            var (_, itemSprite, large) = items[i];
            if (!_composition.TryGetVisualBounds(itemSprite, out var bounds) || bounds.Width <= 0f || bounds.Height <= 0f)
                continue;

            var width = large ? area.X : area.X / smallCount;
            var fit = MathF.Min(width / bounds.Width, area.Y / bounds.Height);
            var position = slot.Offset;
            if (!large)
                position.X += -area.X / 2f + width * (smallIndex++ + 0.5f);

            var scale = new Vector2(fit, fit);
            _composition.CopyLayers(stove, itemSprite, $"adt-stove-oven-item-{index}-{i}", position - bounds.Center * scale, scale, Angle.Zero, null, tint);
        }
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

            if (slot.Type == ADTStoveSlotType.Oven
                && TryComp<ADTCookwareComponent>(cookware, out var ovenCookware)
                && ovenCookware.OvenState != null
                && cookwareSprite.BaseRSI is { } cookwareRsi)
            {
                DrawOvenView(spriteEnt, i, slot, (cookware.Value, ovenCookware), cookwareRsi.Path);
                continue;
            }

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
