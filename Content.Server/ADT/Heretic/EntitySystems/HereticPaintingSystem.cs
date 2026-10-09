using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Server.ADT.Heretic.Abilities;
using Robust.Shared.Map;
using Content.Server.Heretic.Abilities;
using OldStatusEffectsSystem = Content.Shared.StatusEffect.StatusEffectsSystem;
using SharedStatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed partial class HereticPaintingSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly OldStatusEffectsSystem _statusOld = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedStutteringSystem _stutter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    private float _acc;
    private float _rustAcc;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _rustAcc += frameTime;
        if (_rustAcc >= 3f)
        {
            _rustAcc = 0f;
            RustTiles();
        }

        _acc += frameTime;
        if (_acc < 10f)
            return;
        _acc = 0f;

        var query = EntityQueryEnumerator<HereticPaintingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var paint, out var xform))
        {
            foreach (var ent in _lookup.GetEntitiesInRange<MobStateComponent>(xform.Coordinates, 5f))
            {
                if (!_mobState.IsAlive(ent))
                    continue;
                if (_heretic.IsHereticOrGhoul(ent))
                    continue;

                switch (paint.Kind)
                {
                    case HereticPaintingKind.Weeping:
                        _jitter.DoJitter(ent, TimeSpan.FromSeconds(4f), true);
                        break;

                    case HereticPaintingKind.Desire:
                        if (TryComp<HungerComponent>(ent, out var hunger))
                            _hunger.ModifyHunger(ent, -75f, hunger);
                        break;

                    case HereticPaintingKind.Vines:
                        _statusNew.TryAddStatusEffectDuration(ent, "SlowedDown", TimeSpan.FromSeconds(10));
                        break;

                    case HereticPaintingKind.Beauty:
                        Scratch(ent);
                        break;

                    case HereticPaintingKind.Rust:
                        break;
                }
            }
        }
    }

    private void RustTiles()
    {
        var query = EntityQueryEnumerator<HereticPaintingComponent>();
        while (query.MoveNext(out var uid, out var paint))
        {
            if (paint.Kind != HereticPaintingKind.Rust)
                continue;

            foreach (var ent in _lookup.GetEntitiesInRange<MobStateComponent>(Transform(uid).Coordinates, 5f))
            {
                if (!_mobState.IsAlive(ent))
                    continue;
                if (_heretic.IsHereticOrGhoul(ent))
                    continue;

                var coords = _xform.GetMapCoordinates(ent);
                if (!_mapMan.TryFindGridAt(coords, out var gridUid, out var grid))
                    continue;

                var tile = _map.GetTileRef((gridUid, grid), coords);
                var ability = EntitySystem.Get<HereticAbilitySystem>();
                if (ability.CanRustTile((ContentTileDefinition) _tileDef[tile.Tile.TypeId]))
                    ability.MakeRustTile(gridUid, grid, tile, RustTilePrototype);
            }
        }
    }

    private void Scratch(EntityUid ent)
    {
        if (_inventory.TryGetSlotEntity(ent, "outerClothing", out _))
            return;

        _jitter.DoJitter(ent, TimeSpan.FromSeconds(2f), true);
        _stutter.DoStutter(ent, TimeSpan.FromSeconds(6f), true);

        var spec = new DamageSpecifier();
        spec.DamageDict.Add(BluntDamageType, RustDamage);
        _damage.TryChangeDamage(ent, spec, true);
    }

    private static readonly EntProtoId RustTilePrototype = "TileHereticRustRune";

    private const string BluntDamageType = "Blunt";
    private const float RustDamage = 3f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticPaintingComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HereticPaintingComponent> ent, ref ExaminedEvent args)
    {
        var isHeretic = _heretic.IsHereticOrGhoul(args.Examiner);
        switch (ent.Comp.Kind)
        {
            case HereticPaintingKind.Weeping:
                args.PushMarkup(Loc.GetString(isHeretic ? "heretic-painting-weeping-heretic" : "heretic-painting-weeping-heathen"));
                break;
            case HereticPaintingKind.Desire:
                args.PushMarkup(Loc.GetString(isHeretic ? "heretic-painting-desire-heretic" : "heretic-painting-desire-heathen"));
                break;
            case HereticPaintingKind.Vines:
                args.PushMarkup(Loc.GetString(isHeretic ? "heretic-painting-vines-heretic" : "heretic-painting-vines-heathen"));
                break;
            case HereticPaintingKind.Beauty:
                args.PushMarkup(Loc.GetString(isHeretic ? "heretic-painting-beauty-heretic" : "heretic-painting-beauty-heathen"));
                break;
            case HereticPaintingKind.Rust:
                args.PushMarkup(Loc.GetString(isHeretic ? "heretic-painting-rust-heretic" : "heretic-painting-rust-heathen"));
                break;
        }
    }
}
