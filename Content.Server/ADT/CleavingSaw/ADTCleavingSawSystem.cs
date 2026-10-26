using System.Numerics;
using Content.Shared.ADT.CleavingSaw;
using Content.Shared.Clumsy.Components;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Damage.Systems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using NewStatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

namespace Content.Server.ADT.CleavingSaw;

public sealed class ADTCleavingSawSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private NewStatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private static readonly int[] CleaveTurns = { 0, -1, 1 };

    private readonly HashSet<Entity<MobStateComponent>> _cleaveMobs = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTCleavingSawComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTCleavingSawComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<ADTCleavingSawComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<ADTCleavingSawComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ADTCleavingSawComponent> ent, ref ExaminedEvent args)
    {
        var mode = _toggle.IsActivated(ent.Owner) ? "adt-cleaving-saw-examine-open" : "adt-cleaving-saw-examine-closed";
        args.PushMarkup(Loc.GetString(mode));
        args.PushMarkup(Loc.GetString("adt-cleaving-saw-examine-common"));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTSawBleedComponent>();

        while (query.MoveNext(out var uid, out var bleed))
        {
            if (_mobState.IsDead(uid))
            {
                RemCompDeferred<ADTSawBleedComponent>(uid);
                continue;
            }

            if (now < bleed.NextTick)
                continue;

            bleed.NextTick = now + bleed.TickInterval;
            AddBleed((uid, bleed), -1);
        }
    }

    private void OnMapInit(Entity<ADTCleavingSawComponent> ent, ref MapInitEvent args)
    {
        UpdateAttackRate(ent, _toggle.IsActivated(ent.Owner));
    }

    private void OnToggled(Entity<ADTCleavingSawComponent> ent, ref ItemToggledEvent args)
    {
        UpdateAttackRate(ent, args.Activated);

        if (args.User is not { } user)
            return;

        if (TryComp<MeleeWeaponComponent>(ent, out var melee))
        {
            melee.NextAttack = _timing.CurTime + ent.Comp.TransformAttackDelay;
            Dirty(ent, melee);
        }

        if (_statusEffects.HasEffectComp<ClumsyGunStatusEffectComponent>(user) && _random.Prob(ent.Comp.ClumsyChance))
            _damageable.TryChangeDamage(user, ent.Comp.ClumsyDamage, origin: user);
    }

    private void UpdateAttackRate(Entity<ADTCleavingSawComponent> ent, bool open)
    {
        if (!TryComp<MeleeWeaponComponent>(ent, out var melee))
            return;

        melee.AttackRate = open ? ent.Comp.OpenAttackRate : ent.Comp.ClosedAttackRate;
        Dirty(ent, melee);
    }

    private void OnMeleeHit(Entity<ADTCleavingSawComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (args.Iswide || args.Direction != null)
        {
            WideHit(ent, args);
            return;
        }

        EntityUid? target = null;

        foreach (var hit in args.HitEntities)
        {
            if (hit == args.User || !HasComp<MobStateComponent>(hit))
                continue;

            target = hit;
            break;
        }

        if (target is not { } victim)
            return;

        var open = _toggle.IsActivated(ent.Owner);

        if (IsNemesis(ent, victim))
        {
            if (open)
                args.BonusDamage += ent.Comp.NemesisBonusDamage;

            ApplyNemesisEffects(ent, victim, args.User, open);
        }

        if (open)
            Cleave(ent, args.User, victim, args.BaseDamage);
    }

    private void WideHit(Entity<ADTCleavingSawComponent> ent, MeleeHitEvent args)
    {
        var open = _toggle.IsActivated(ent.Owner);
        var clickModifier = (float) _melee.GetHeavyDamageModifier(ent.Owner, args.User);
        var bonus = clickModifier > 0f
            ? ent.Comp.NemesisBonusDamage * (1f / clickModifier)
            : ent.Comp.NemesisBonusDamage;

        foreach (var hit in args.HitEntities)
        {
            if (hit == args.User || !HasComp<MobStateComponent>(hit) || !IsNemesis(ent, hit))
                continue;

            if (open)
                _damageable.TryChangeDamage(hit, bonus, origin: args.User);

            ApplyNemesisEffects(ent, hit, args.User, open);
        }
    }

    private void Cleave(Entity<ADTCleavingSawComponent> ent, EntityUid user, EntityUid target, DamageSpecifier baseDamage)
    {
        var userXform = Transform(user);

        if (userXform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var userTile = _map.TileIndicesFor(gridUid, grid, userXform.Coordinates);
        var targetTile = _map.TileIndicesFor(gridUid, grid, Transform(target).Coordinates);

        if (userTile == targetTile)
            return;

        var dir = ((Vector2) (targetTile - userTile)).GetDir();

        foreach (var turn in CleaveTurns)
        {
            var checkDir = (Direction) (((int) dir + turn + 8) % 8);
            var checkTile = userTile + checkDir.ToIntVec();

            _cleaveMobs.Clear();
            _lookup.GetLocalEntitiesIntersecting(gridUid, checkTile, _cleaveMobs, flags: LookupFlags.Uncontained, gridComp: grid);

            foreach (var mob in _cleaveMobs)
            {
                if (mob.Owner == user || mob.Owner == target || _standing.IsDown(mob.Owner))
                    continue;

                var damage = baseDamage;
                var nemesis = IsNemesis(ent, mob);

                if (nemesis)
                    damage += ent.Comp.NemesisBonusDamage;

                _damageable.TryChangeDamage(mob.Owner, damage, origin: user);

                if (nemesis)
                    ApplyNemesisEffects(ent, mob, user, true);
            }
        }
    }

    private bool IsNemesis(Entity<ADTCleavingSawComponent> ent, EntityUid target)
    {
        return _whitelist.IsWhitelistPass(ent.Comp.NemesisWhitelist, target);
    }

    private void ApplyNemesisEffects(Entity<ADTCleavingSawComponent> ent, EntityUid target, EntityUid user, bool open)
    {
        if (TryComp<ADTSawBleedComponent>(target, out var existing))
        {
            AddBleed((target, existing), ent.Comp.BleedBuildup);
            return;
        }

        if (open || _mobState.IsDead(target))
            return;

        var bleed = AddComp<ADTSawBleedComponent>(target);
        bleed.Amount = ent.Comp.BleedStart;
        bleed.Cap = ent.Comp.BleedCap;
        bleed.TickInterval = ent.Comp.BleedTickInterval;
        bleed.DecayDelay = ent.Comp.BleedDecayDelay;
        bleed.NextTick = _timing.CurTime + ent.Comp.BleedTickInterval;
        bleed.Damage = ent.Comp.BleedDamage;
        bleed.BurstSound = ent.Comp.BleedBurstSound;
        bleed.BurstEffect = ent.Comp.BleedBurstEffect;
        bleed.Source = user;
    }

    private void AddBleed(Entity<ADTSawBleedComponent> ent, int amount)
    {
        ent.Comp.Amount += amount;

        if (ent.Comp.Amount <= 0)
        {
            RemCompDeferred<ADTSawBleedComponent>(ent);
            return;
        }

        if (ent.Comp.Amount >= ent.Comp.Cap)
        {
            Burst(ent);
            RemCompDeferred<ADTSawBleedComponent>(ent);
            return;
        }

        if (amount > 0)
            ent.Comp.NextTick += ent.Comp.DecayDelay;
    }

    private void Burst(Entity<ADTSawBleedComponent> ent)
    {
        var coords = _transform.GetMoverCoordinates(ent);

        if (ent.Comp.BurstEffect is { } effect)
            Spawn(effect, coords);

        if (ent.Comp.BurstSound != null)
            _audio.PlayPvs(ent.Comp.BurstSound, coords);

        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, true, origin: ent.Comp.Source);
    }
}
