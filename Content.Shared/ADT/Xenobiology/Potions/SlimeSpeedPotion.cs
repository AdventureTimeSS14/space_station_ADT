using Content.Shared.Clothing;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that halves the walk and sprint penalty of clothing.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeSpeedPotionComponent : Component;

public sealed partial class SlimeSpeedPotionSystem : EntitySystem
{
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifier = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeSpeedPotionComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SlimeSpeedPotionComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnAfterInteract(Entity<SlimeSpeedPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryModifyWalkSpeed(target, args.User))
            return;

        args.Handled = true;
        PredictedQueueDel(args.Used);
    }

    private bool TryModifyWalkSpeed(EntityUid target, EntityUid user)
    {
        if (!TryComp<ClothingSpeedModifierComponent>(target, out var clothing))
            return false;

        _clothingSpeedModifier.SetWalkSpeedModifier(clothing, 1.0f);
        _clothingSpeedModifier.SetSprintSpeedModifier(clothing, 1.0f);
        Dirty(target, clothing);

        _movementSpeed.RefreshMovementSpeedModifiers(target);
        if (_container.TryGetContainingContainer((target, null, null), out var container))
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);

        _popup.PopupPredicted(Loc.GetString("xeno-potion-speed-applied",
            ("name", Name(target)),
            ("walk", clothing.WalkModifier),
            ("sprint", clothing.SprintModifier)), user, user);
        return true;
    }

    private void OnUtilityVerb(Entity<SlimeSpeedPotionComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (args.Target is not { Valid: true } target || !args.CanAccess)
            return;

        var user = args.User;
        var verb = new UtilityVerb
        {
            Act = () =>
            {
                if (TryModifyWalkSpeed(target, user))
                    PredictedQueueDel(ent.Owner);
            },
            Text = Loc.GetString("xeno-potion-speed-verb"),
        };

        args.Verbs.Add(verb);
    }
}
