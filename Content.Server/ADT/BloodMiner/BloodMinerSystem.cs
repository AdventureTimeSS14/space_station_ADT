using System.Linq;
using Content.Shared.ADT.BloodMiner;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BloodMiner;

public sealed class BloodMinerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodMinerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BloodMinerComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<BloodMinerComponent, DamageDealtEvent>(OnDamageDealt);
    }

    private void OnMapInit(Entity<BloodMinerComponent> ent, ref MapInitEvent args)
    {
        ApplySawState(ent);
    }

    private void OnDamageDealt(Entity<BloodMinerComponent> ent, ref DamageDealtEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(ent, out var melee))
            return;

        var total = (float)args.Damage.GetTotal();
        if (total <= 0f)
            return;

        var delay = TimeSpan.FromSeconds(total * ent.Comp.DamageInterruptCoefficient);
        var next = _timing.CurTime + delay;
        if (next <= melee.NextAttack)
            return;

        melee.NextAttack = next;
        Dirty(ent.Owner, melee);
    }

    private void OnMeleeHit(Entity<BloodMinerComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        var butchered = false;
        foreach (var target in args.HitEntities)
        {
            if (target == ent.Owner)
                continue;

            if (!HasComp<MobStateComponent>(target) || !_mobState.IsDead(target))
                continue;

            Butcher(ent, target);
            butchered = true;
        }

        if (butchered)
            return;

        if (ent.Comp.SawOpen)
            Cleave(ent, args);

        if (_random.Prob(ent.Comp.TransformAfterAttackChance))
            TryTransformSaw(ent);
    }

    private void Cleave(Entity<BloodMinerComponent> ent, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (!TryComp<MeleeWeaponComponent>(ent, out var melee))
            return;

        var origin = _transform.GetMapCoordinates(ent);
        if (origin.MapId == MapId.Nullspace)
            return;

        var primary = args.HitEntities[0];
        var toPrimary = _transform.GetMapCoordinates(primary).Position - origin.Position;
        if (toPrimary.LengthSquared() < 0.001f)
            return;

        var attackAngle = new Angle(toPrimary);
        var arc = Angle.FromDegrees(ent.Comp.CleaveArc);

        var candidates = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(origin, melee.Range, candidates);

        foreach (var candidate in candidates)
        {
            if (candidate.Owner == ent.Owner || args.HitEntities.Contains(candidate.Owner))
                continue;

            if (_mobState.IsDead(candidate))
                continue;

            var offset = _transform.GetMapCoordinates(candidate.Owner).Position - origin.Position;
            if (offset.LengthSquared() < 0.001f)
                continue;

            if (Math.Abs(Angle.ShortestDistance(attackAngle, new Angle(offset)).Degrees) > arc.Degrees)
                continue;

            _damageable.TryChangeDamage(candidate.Owner, melee.Damage, origin: ent.Owner);
        }
    }

    private void Butcher(Entity<BloodMinerComponent> ent, EntityUid target)
    {
        if (_thresholds.TryGetDeadThreshold(target, out var maxHealth))
        {
            var heal = new DamageSpecifier();
            heal.DamageDict.Add("Blunt", -(float)maxHealth.Value * ent.Comp.ButcherHealFraction);
            _damageable.TryChangeDamage(ent.Owner, heal, true, origin: ent.Owner);
        }

        _popup.PopupEntity(Loc.GetString("adt-blood-miner-butcher", ("target", target)), ent, PopupType.LargeCaution);

        if (_gibbing.Gib(target, true, ent.Owner).Count == 0)
            QueueDel(target);
    }

    public bool TryTransformSaw(Entity<BloodMinerComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.NextTransformAt)
            return false;

        ent.Comp.SawOpen = !ent.Comp.SawOpen;
        ent.Comp.NextTransformAt = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(
            (float)ent.Comp.TransformCooldownMin.TotalSeconds,
            (float)ent.Comp.TransformCooldownMax.TotalSeconds));

        ApplySawState(ent);
        _audio.PlayPvs(ent.Comp.TransformSound, ent);
        return true;
    }

    private void ApplySawState(Entity<BloodMinerComponent> ent)
    {
        if (TryComp<MeleeWeaponComponent>(ent, out var melee))
        {
            melee.Damage = new DamageSpecifier(ent.Comp.SawOpen ? ent.Comp.OpenDamage : ent.Comp.ClosedDamage);
            melee.AttackRate = ent.Comp.SawOpen ? ent.Comp.OpenAttackRate : ent.Comp.ClosedAttackRate;
            Dirty(ent.Owner, melee);
        }

        _appearance.SetData(ent, BloodMinerVisuals.SawOpen, ent.Comp.SawOpen);
        Dirty(ent);
    }
}
