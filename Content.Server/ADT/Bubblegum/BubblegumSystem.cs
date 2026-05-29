using Content.Shared.ADT.Bubblegum;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Eye;
using Content.Shared.Fluids.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum;

public sealed class BubblegumSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BubblegumComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<BubblegumComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BubblegumComponent, ProjectileHitAttemptEvent>(OnProjectileHitAttempt);
        SubscribeLocalEvent<BubblegumComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<BubblegumComponent, GetVisMaskEvent>(OnGetVisMask);
    }

    private void OnStartup(Entity<BubblegumComponent> ent, ref ComponentStartup args)
    {
        _eye.RefreshVisibilityMask(ent.Owner);
    }

    private void OnGetVisMask(Entity<BubblegumComponent> ent, ref GetVisMaskEvent args)
    {
        args.VisibilityMask |= (int)VisibilityFlags.Bubblegum;
    }

    private void OnMove(Entity<BubblegumComponent> ent, ref MoveEvent args)
    {
        if (args.ParentChanged)
            return;

        if (args.OldPosition.Position == args.NewPosition.Position)
            return;

        if (_mobState.IsDead(ent))
            return;

        if (!HasPuddleHere(args.NewPosition))
            SpawnAtCoords(ent.Comp.BloodPrototype, args.NewPosition);

        if (_timing.CurTime - ent.Comp.LastStepSound < ent.Comp.StepSoundCooldown)
            return;

        ent.Comp.LastStepSound = _timing.CurTime;
        _audio.PlayPvs(ent.Comp.StepSound, ent);
    }

    private bool HasPuddleHere(EntityCoordinates coords)
    {
        if (!coords.IsValid(EntityManager))
            return false;

        var map = _transform.ToMapCoordinates(coords);
        if (map.MapId == MapId.Nullspace)
            return false;

        return _lookup.GetEntitiesInRange<PuddleComponent>(map, 0.1f).Count > 0;
    }

    private void OnDamageChanged(Entity<BubblegumComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (TryComp<MobThresholdsComponent>(ent, out var thresholds))
        {
            var maxHealth = 0f;
            foreach (var (damage, _) in thresholds.Thresholds)
            {
                if ((float)damage > maxHealth)
                    maxHealth = (float)damage;
            }

            var damageTaken = (float)args.Damageable.TotalDamage;
            ent.Comp.AngerModifier = Math.Clamp(damageTaken / 60f, 0f, 20f);

            if (!ent.Comp.InSmashPhase && damageTaken >= maxHealth * 0.5f)
                ent.Comp.InSmashPhase = true;
        }

        if (_random.Prob(ent.Comp.ThickBloodOnDamageChance))
        {
            SpawnAtCoords(ent.Comp.ThickBloodPrototype, _transform.GetMoverCoordinates(ent));
        }
    }

    private void OnProjectileHitAttempt(Entity<BubblegumComponent> ent, ref ProjectileHitAttemptEvent args)
    {
        if (!IsEnraged(ent))
            return;

        args.Cancel();
        _audio.PlayPvs(ent.Comp.RangedDeflectSound, ent);
    }

    private void OnRefreshSpeed(Entity<BubblegumComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!IsEnraged(ent))
            return;

        args.ModifySpeed(ent.Comp.EnragedSpeedMultiplier, ent.Comp.EnragedSpeedMultiplier);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BubblegumComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EnrageEndsAt == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < comp.EnrageEndsAt)
                continue;

            ExitEnrage((uid, comp));
        }
    }

    public bool IsEnraged(Entity<BubblegumComponent> ent)
    {
        return ent.Comp.EnrageEndsAt > _timing.CurTime;
    }

    public bool CanEnrage(Entity<BubblegumComponent> ent)
    {
        return _timing.CurTime >= ent.Comp.NextEnrageAvailableAt;
    }

    public bool TryEnterEnrage(Entity<BubblegumComponent> ent)
    {
        if (!CanEnrage(ent))
            return false;

        var duration = TimeSpan.FromSeconds(ent.Comp.BaseEnrageDuration.TotalSeconds *
                                            Math.Clamp(ent.Comp.AngerModifier / 20f, 0.5f, 1f));
        ent.Comp.EnrageEndsAt = _timing.CurTime + duration;
        ent.Comp.NextEnrageAvailableAt = _timing.CurTime + duration * 2;

        if (ent.Comp.HealingReceivedFromEnrage < ent.Comp.MaxHealingFromEnrage && ent.Comp.EnrageHeal > 0)
        {
            var heal = Math.Min(ent.Comp.EnrageHeal, ent.Comp.MaxHealingFromEnrage - ent.Comp.HealingReceivedFromEnrage);
            ent.Comp.HealingReceivedFromEnrage += heal;
            var healing = new DamageSpecifier();
            healing.DamageDict.Add("Blunt", -heal);
            _damageable.TryChangeDamage(ent.Owner, healing, true);
        }

        _appearance.SetData(ent, BubblegumVisuals.Enraged, true);
        _speed.RefreshMovementSpeedModifiers(ent);

        Dirty(ent);
        return true;
    }

    private void ExitEnrage(Entity<BubblegumComponent> ent)
    {
        ent.Comp.EnrageEndsAt = TimeSpan.Zero;
        _appearance.SetData(ent, BubblegumVisuals.Enraged, false);
        _speed.RefreshMovementSpeedModifiers(ent);
        Dirty(ent);
    }

    public EntityUid? SpawnAtCoords(string proto, EntityCoordinates coords)
    {
        if (!coords.IsValid(EntityManager))
            return null;

        return SpawnAtPosition(proto, coords);
    }

    public EntityUid? SpawnAtCoords(string proto, MapCoordinates coords)
    {
        if (coords.MapId == MapId.Nullspace)
            return null;

        return Spawn(proto, coords);
    }
}
