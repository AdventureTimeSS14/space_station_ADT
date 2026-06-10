using Content.Server.ADT.Sandevistan;
using Content.Shared.ADT.Abilities;
using Content.Shared.ADT.KereznikovImplant;
using Content.Shared.ADT.Sandevistan;
using Content.Shared.ADT.Trail;
using Content.Shared.Implants;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.ADT.KereznikovImplant;

public sealed class KereznikovImplantSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KereznikovImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<KereznikovImplantComponent, ImplantRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<KereznikovImplantComponent, ActivateKereznikovActionEvent>(OnActivate);

        SubscribeLocalEvent<KereznikovActiveComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<KereznikovActiveComponent, ComponentRemove>(OnActiveRemove);
        SubscribeLocalEvent<KereznikovActiveComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<KereznikovActiveComponent, MeleeAttackEvent>(OnMeleeAttack);
        SubscribeLocalEvent<KereznikovActiveComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var toRemove = new ValueList<EntityUid>();
        var query = EntityQueryEnumerator<KereznikovActiveComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Trail != null)
            {
                comp.Trail.Color = Color.FromHsv(new Vector4(comp.ColorAccumulator % 100f / 100f, 1, 1, 1));
                comp.ColorAccumulator++;
                Dirty(uid, comp.Trail);
            }

            if (comp.EndTime > curTime)
                continue;

            toRemove.Add(uid);
        }

        foreach (var uid in toRemove)
        {
            RemComp<KereznikovActiveComponent>(uid);
            _speed.RefreshMovementSpeedModifiers(uid);
        }
    }

    private void OnImplanted(Entity<KereznikovImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (HasComp<SandevistanUserComponent>(args.Implanted))
        {
            _popup.PopupEntity(Loc.GetString("kereznikov-incompatible-sandevistan"), args.Implanted, args.Implanted);
            _implants.ForceRemove(args.Implanted, ent.Owner);
            return;
        }

        EnsureComp<KereznikovImplantedComponent>(args.Implanted);
    }

    private void OnRemoved(Entity<KereznikovImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<KereznikovImplantedComponent>(args.Implanted);
        RemComp<KereznikovActiveComponent>(args.Implanted);
    }

    private void OnActivate(Entity<KereznikovImplantComponent> ent, ref ActivateKereznikovActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        if (HasComp<ActiveSandevistanUserComponent>(user) || HasComp<KereznikovActiveComponent>(user))
            return;

        args.Handled = true;

        var active = EnsureComp<KereznikovActiveComponent>(user);
        active.EndTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Duration);
        active.MovementSpeedModifier = ent.Comp.MovementSpeedModifier;
        active.AttackSpeedModifier = ent.Comp.AttackSpeedModifier;

        _speed.RefreshMovementSpeedModifiers(user);
    }

    private void OnActiveInit(Entity<KereznikovActiveComponent> ent, ref ComponentInit args)
    {
        EnsureComp<SandevistanVisionComponent>(ent.Owner);

        if (!HasComp<TrailComponent>(ent.Owner))
        {
            var trail = AddComp<TrailComponent>(ent.Owner);
            trail.RenderedEntity = ent.Owner;
            trail.LerpTime = 0.05f;
            trail.LerpDelay = TimeSpan.FromSeconds(1);
            trail.Lifetime = 0.5f;
            trail.Frequency = 0.06f;
            trail.AlphaLerpAmount = 0.3f;
            trail.MaxParticleAmount = 15;
            ent.Comp.Trail = trail;
        }
    }

    private void OnActiveRemove(Entity<KereznikovActiveComponent> ent, ref ComponentRemove args)
    {
        RemComp<SandevistanVisionComponent>(ent.Owner);
        RemComp<TrailComponent>(ent.Owner);
        ent.Comp.Trail = null;
    }

    private void OnRefreshSpeed(Entity<KereznikovActiveComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.MovementSpeedModifier, ent.Comp.MovementSpeedModifier);
    }

    private void OnMeleeAttack(Entity<KereznikovActiveComponent> ent, ref MeleeAttackEvent args)
    {
        if (!TryComp<MeleeWeaponComponent>(args.Weapon, out var weapon))
            return;

        var rate = weapon.NextAttack - _timing.CurTime;
        weapon.NextAttack -= rate - rate / ent.Comp.AttackSpeedModifier;
    }

    private void OnMobStateChanged(Entity<KereznikovActiveComponent> ent, ref MobStateChangedEvent args)
    {
        RemComp<KereznikovActiveComponent>(ent.Owner);
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }
}
