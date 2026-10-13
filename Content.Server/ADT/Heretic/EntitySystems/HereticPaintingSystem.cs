using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.ADT.Heretic;
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
using Content.Shared.Popups;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.UserInterface;
using Content.Shared.Stunnable;
using Content.Shared.Station;
using Content.Server.ADT.Heretic.Abilities;
using Robust.Shared.Map;
using Content.Server.Heretic.Abilities;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using OldStatusEffectsSystem = Content.Shared.StatusEffect.StatusEffectsSystem;
using SharedStatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Player;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed partial class HereticPaintingSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _statusNew = default!;
    [Dependency] private readonly OldStatusEffectsSystem _statusOld = default!;
    [Dependency] private readonly SatiationSystem _satiation = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly StutteringSystem _stutter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _acc;
    private float _blessAcc;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _blessAcc += frameTime;
        if (_blessAcc >= 1f)
        {
            _blessAcc = 0f;
            TickBlessings();
        }

        _acc += frameTime;
        if (_acc < 10f)
            return;
        _acc = 0f;

        var query = EntityQueryEnumerator<HereticPaintingCurseComponent>();
        while (query.MoveNext(out var uid, out var curse))
        {
            curse.TimeLeft -= 10f;
            if (curse.TimeLeft <= 0f || TerminatingOrDeleted(uid))
            {
                RemCompDeferred<HereticPaintingCurseComponent>(uid);
                continue;
            }

            if (!_mobState.IsAlive(uid))
                continue;

            ApplyCurse(uid, curse.Kind);
            Dirty(uid, curse);
        }
    }


    private void TickBlessings()
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<HereticPaintingBlessingComponent>();
        while (query.MoveNext(out var uid, out var bless))
        {
            bless.TimeLeft -= 1f;
            if (bless.TimeLeft <= 0f || TerminatingOrDeleted(uid))
            {
                RemCompDeferred<HereticPaintingBlessingComponent>(uid);
                continue;
            }

            var actions = EntityQueryEnumerator<ActionComponent>();
            while (actions.MoveNext(out var actionUid, out var action))
            {
                if (action.AttachedEntity != uid)
                    continue;

                if (action.Cooldown == null)
                {
                    bless.Processed.Remove(actionUid);
                    continue;
                }

                if (bless.Processed.TryGetValue(actionUid, out var seen) && seen == action.Cooldown.Value.Start)
                    continue;

                bless.Processed[actionUid] = action.Cooldown.Value.Start;
                var reduced = action.Cooldown.Value.End - TimeSpan.FromSeconds(5);
                Entity<Content.Shared.Actions.Components.ActionComponent?> act = (actionUid, action);
                if (reduced <= now)
                    _actions.ClearCooldown(act);
                else
                    _actions.SetCooldown(act, action.Cooldown.Value.Start, reduced);
            }

            Dirty(uid, bless);
        }
    }

    private void ApplyCurse(EntityUid victim, HereticPaintingKind kind)
    {
        switch (kind)
        {
            case HereticPaintingKind.Weeping:
                _jitter.DoJitter(victim, TimeSpan.FromSeconds(4f), true);
                break;

            case HereticPaintingKind.Desire:
                if (TryComp<SatiationComponent>(victim, out var satiation))
                    _satiation.ModifyValue((victim, satiation), SatiationSystem.Hunger, -75f);
                break;

            case HereticPaintingKind.Vines:
                _statusNew.TryAddStatusEffectDuration(victim, "SlowedDown", TimeSpan.FromSeconds(10));
                break;

            case HereticPaintingKind.Beauty:
                Scratch(victim);
                break;

            case HereticPaintingKind.Rust:
                RustUnder(victim);
                break;
        }
    }

    private void RustUnder(EntityUid victim)
    {
        var coords = _xform.GetMapCoordinates(victim);
        if (!_map.TryFindGridAt(coords, out var gridUid, out var grid))
            return;

        var tile = _map.GetTileRef((gridUid, grid), coords);
        var ability = EntitySystem.Get<HereticAbilitySystem>();
        if (ability.CanRustTile((ContentTileDefinition) _tileDef[tile.Tile.TypeId]))
            ability.MakeRustTile(gridUid, grid, tile, RustTilePrototype);
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
        SubscribeLocalEvent<HereticPaintingComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<HereticPaintingComponent, PaintingCurseSelectMessage>(OnSelect);
        SubscribeLocalEvent<HereticPaintingComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    private void OnUiOpened(Entity<HereticPaintingComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Actor))
        {
            _ui.CloseUi(ent.Owner, PaintingCurseUiKey.Key, args.Actor);
            return;
        }

        var targets = new List<PaintingCurseTargetInfo>();
        ent.Comp.ValidTargets.Clear();

        var query = EntityQueryEnumerator<ActorComponent, MobStateComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out _, out var meta))
        {
            if (uid == args.Actor)
                continue;

            if (!_mobState.IsAlive(uid))
                continue;

            if (_heretic.IsHereticOrGhoul(uid))
                continue;

            if (_station.GetOwningStation(uid) == null)
                continue;

            var netEntity = GetNetEntity(uid);
            ent.Comp.ValidTargets.Add(netEntity);

            targets.Add(new PaintingCurseTargetInfo
            {
                Entity = netEntity,
                Name = meta.EntityName,
            });
        }

        _ui.SetUiState(ent.Owner, PaintingCurseUiKey.Key, new PaintingCurseBuiState(targets));
    }

    private void OnSelect(Entity<HereticPaintingComponent> ent, ref PaintingCurseSelectMessage args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Actor))
            return;

        if (!ent.Comp.ValidTargets.Contains(args.Target))
            return;

        var victim = GetEntity(args.Target);
        if (!Exists(victim) || victim == args.Actor)
            return;

        if (!HasComp<MobStateComponent>(victim) || !_mobState.IsAlive(victim))
            return;

        ent.Comp.ValidTargets.Clear();
        _ui.CloseUi(ent.Owner, PaintingCurseUiKey.Key);

        var curse = EnsureComp<HereticPaintingCurseComponent>(victim);
        curse.Kind = ent.Comp.Kind;
        curse.TimeLeft = 120f;
        Dirty(victim, curse);

        _popup.PopupEntity(Loc.GetString("heretic-painting-cursed", ("target", victim)), args.Actor, args.Actor, PopupType.Medium);
        BlessHeretic(args.Actor, ent.Comp.Kind);
        QueueDel(ent.Owner);
    }

    private void OnUiClosed(Entity<HereticPaintingComponent> ent, ref BoundUIClosedEvent args)
    {
        ent.Comp.ValidTargets.Clear();
    }


    private void BlessHeretic(EntityUid heretic, HereticPaintingKind kind)
    {
        switch (kind)
        {
            case HereticPaintingKind.Weeping:
                var sanity = EnsureComp<HereticSanityComponent>(heretic);
                sanity.Current = Math.Min(sanity.Max, sanity.Current + 50f);
                Dirty(heretic, sanity);
                if (TryComp(heretic, out StatusEffectsComponent? weepStatus))
                    _statusOld.TryRemoveStatusEffect(heretic, "Jitter", weepStatus);
                break;

            case HereticPaintingKind.Desire:
                if (TryComp<SatiationComponent>(heretic, out var satiation))
                    _satiation.ModifyValue((heretic, satiation), SatiationSystem.Hunger, 500f);
                break;

            case HereticPaintingKind.Vines:
                _statusNew.TryRemoveStatusEffect(heretic, "SlowedDown");
                _statusNew.TryAddStatusEffectDuration(heretic, "ReagentSpeedStatusEffect", TimeSpan.FromSeconds(30));
                break;

            case HereticPaintingKind.Beauty:
                var bless = EnsureComp<HereticPaintingBlessingComponent>(heretic);
                bless.TimeLeft = 120f;
                bless.Processed.Clear();
                Dirty(heretic, bless);
                break;

            case HereticPaintingKind.Rust:
                _statusNew.TryRemoveStatusEffect(heretic, "SlowedDown");
                if (TryComp(heretic, out StatusEffectsComponent? rustStatus))
                    _statusOld.TryRemoveStatusEffect(heretic, "Jitter", rustStatus);
                break;
        }
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

        if (isHeretic)
            args.PushMarkup(Loc.GetString("heretic-painting-choose"));
    }
}
