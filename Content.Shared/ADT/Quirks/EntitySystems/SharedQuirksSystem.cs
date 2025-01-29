using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Climbing.Events;
using Robust.Shared.Network;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Tools.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Damage;

namespace Content.Shared.ADT.Traits;

public abstract class SharedQuirksSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityStorageSystem _storage = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FreerunningComponent, CheckClimbSpeedModifiersEvent>(OnFreerunningClimbTimeModify);

        SubscribeLocalEvent<SprinterComponent, MapInitEvent>(OnSprinterMapInit);
        SubscribeLocalEvent<SprinterComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        SubscribeLocalEvent<HardThrowerComponent, CheckThrowRangeModifiersEvent>(OnThrowerRangeModify);

        SubscribeLocalEvent<FrailComponent, DamageModifyEvent>(OnFrailDamaged);
    }

    public void TryHide(EntityUid uid, EntityUid closet)
    {
        if (_storage.Insert(uid, closet))
        {
            _popup.PopupClient(Loc.GetString("quirk-fast-locker-hide-success"), uid);
        }
        else
            _popup.PopupCursor(Loc.GetString("quirk-fast-locker-hide-fail"), uid);
    }

    private void OnFreerunningClimbTimeModify(EntityUid uid, FreerunningComponent comp, ref CheckClimbSpeedModifiersEvent args)
    {
        if (args.User == args.Climber)
            args.Time *= comp.Modifier;
    }

    private void OnSprinterMapInit(EntityUid uid, SprinterComponent comp, MapInitEvent args)
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
        args.SpeedMod = component.Modifier;
        args.VectorMod = component.Modifier;
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
