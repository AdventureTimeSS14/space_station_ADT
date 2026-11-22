using Content.Shared.Clothing;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that halves the walk and sprint penalty of clothing.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeSpeedPotionComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(15);
}

public sealed partial class SlimeSpeedPotionSystem : EntitySystem
{
    [Dependency] private ClothingSpeedModifierSystem _clothingSpeedModifier = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;

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

        if (!TryModifyWalkSpeed(ent, target, args.User))
            return;

        args.Handled = true;
        PredictedQueueDel(args.Used);
    }

    private bool TryModifyWalkSpeed(Entity<SlimeSpeedPotionComponent> potion, EntityUid target, EntityUid user)
    {
        if (!TryComp<ClothingSpeedModifierComponent>(target, out var clothing))
            return false;

        if (!TryComp<SlimeSpeedPotionAppliedComponent>(target, out var applied))
        {
            applied = AddComp<SlimeSpeedPotionAppliedComponent>(target);
            applied.OriginalWalkModifier = clothing.WalkModifier;
            applied.OriginalSprintModifier = clothing.SprintModifier;
        }

        applied.EndTime = _timing.CurTime + potion.Comp.Duration;
        Dirty(target, applied);

        _clothingSpeedModifier.SetWalkSpeedModifier(clothing, 1.0f);
        _clothingSpeedModifier.SetSprintSpeedModifier(clothing, 1.0f);
        Dirty(target, clothing);

        _movementSpeed.RefreshMovementSpeedModifiers(target);
        if (_container.TryGetContainingContainer((target, null, null), out var container))
            _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);

        _popup.PopupPredicted(Loc.GetString("xeno-potion-speed-applied",
            ("name", Name(target)),
            ("walk", clothing.WalkModifier),
            ("sprint", clothing.SprintModifier),
            ("minutes", (int) potion.Comp.Duration.TotalMinutes)), user, user);
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
                if (TryModifyWalkSpeed(ent, target, user))
                    PredictedQueueDel(ent.Owner);
            },
            Text = Loc.GetString("xeno-potion-speed-verb"),
        };

        args.Verbs.Add(verb);
    }

    public void RestoreSpeed(Entity<SlimeSpeedPotionAppliedComponent> ent)
    {
        if (TryComp<ClothingSpeedModifierComponent>(ent, out var clothing))
        {
            _clothingSpeedModifier.SetWalkSpeedModifier(clothing, ent.Comp.OriginalWalkModifier);
            _clothingSpeedModifier.SetSprintSpeedModifier(clothing, ent.Comp.OriginalSprintModifier);
            Dirty(ent, clothing);

            _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);
            if (_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
                _movementSpeed.RefreshMovementSpeedModifiers(container.Owner);
        }

        RemComp<SlimeSpeedPotionAppliedComponent>(ent);
    }
}
