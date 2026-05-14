using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Climbing.Events;
using Content.Shared.Throwing;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Shared.ADT.Traits;

public abstract class SharedQuirksSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FreerunningComponent, CheckClimbSpeedModifiersEvent>(OnFreerunningClimbTimeModify);

        SubscribeLocalEvent<SprinterComponent, ComponentInit>(OnSprinterComponentInit);
        SubscribeLocalEvent<SprinterComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        SubscribeLocalEvent<HardThrowerComponent, CheckThrowRangeModifiersEvent>(OnThrowerRangeModify);

        SubscribeLocalEvent<FrailComponent, DamageModifyEvent>(OnFrailDamaged);
    }

    private void OnFreerunningClimbTimeModify(EntityUid uid, FreerunningComponent comp, ref CheckClimbSpeedModifiersEvent args)
    {
        if (args.User == args.Climber)
            args.Time *= comp.Modifier;
    }

    private void OnSprinterComponentInit(EntityUid uid, SprinterComponent comp, ComponentInit args)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var move))
            return;
        _movementSpeed.RefreshMovementSpeedModifiers(uid, move);
    }
    private void OnRefreshMovespeed(EntityUid uid, SprinterComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(1f, component.Modifier);
    }

    private void OnThrowerRangeModify(EntityUid uid, HardThrowerComponent component, ref CheckThrowRangeModifiersEvent args)
    {
        args.SpeedMod *= component.Modifier;
        args.VectorMod *= component.Modifier;
    }

    private void OnFrailDamaged(EntityUid uid, FrailComponent comp, DamageModifyEvent args)
    {
        foreach (var type in args.Damage.DamageDict.Keys)
        {
            if (comp.Modifiers.ContainsKey(type))
                args.Damage.DamageDict[type] = args.Damage.DamageDict[type] * comp.Modifiers[type];
        }
    }
}
