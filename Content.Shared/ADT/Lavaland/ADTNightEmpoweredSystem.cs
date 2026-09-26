using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Lavaland;

public sealed class ADTNightEmpoweredSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTNightEmpoweredComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTNightEmpoweredComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTNightEmpoweredComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<ADTNightEmpoweredComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ADTNightEmpoweredComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnStartup(Entity<ADTNightEmpoweredComponent> ent, ref ComponentStartup args)
    {
        _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnShutdown(Entity<ADTNightEmpoweredComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent))
            _speed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnGetMeleeDamage(Entity<ADTNightEmpoweredComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (args.Weapon != ent.Owner)
            return;

        args.Damage *= ent.Comp.MeleeDamageMultiplier;
    }

    private void OnDamageModify(Entity<ADTNightEmpoweredComponent> ent, ref DamageModifyEvent args)
    {
        args.Damage *= ent.Comp.IncomingDamageMultiplier;
    }

    private void OnRefreshSpeed(Entity<ADTNightEmpoweredComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.Running)
            return;

        args.ModifySpeed(ent.Comp.SpeedMultiplier);
    }
}
