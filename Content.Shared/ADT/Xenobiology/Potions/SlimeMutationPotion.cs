using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that raises a slime's mutation chance.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeMutationPotionComponent : Component
{
    public const float MutationChangeAmount = 0.12f;
}

public sealed partial class SlimeMutationPotionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeMutationPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeMutationPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<SlimeComponent>(target, out var slime))
            return;

        args.Handled = true;

        if (slime.MutationChance >= 1f)
        {
            _popup.PopupPredicted(Loc.GetString("xeno-potion-mutation-max", ("name", Name(target))), args.User, args.User);
            return;
        }

        slime.MutationChance = Math.Clamp(slime.MutationChance + SlimeMutationPotionComponent.MutationChangeAmount, 0f, 1f);
        _popup.PopupPredicted(Loc.GetString("xeno-potion-mutation-applied",
            ("name", Name(target)),
            ("chance", (int) (slime.MutationChance * 100))), args.User, args.User);
        PredictedQueueDel(args.Used);
    }
}
