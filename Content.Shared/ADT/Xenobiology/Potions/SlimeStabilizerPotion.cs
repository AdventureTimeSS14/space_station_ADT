using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that lowers a slime's mutation chance.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeStabilizerPotionComponent : Component
{
    public const float MutationChangeAmount = -0.15f;
}

public sealed partial class SlimeStabilizerPotionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeStabilizerPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeStabilizerPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<SlimeComponent>(target, out var slime))
            return;

        args.Handled = true;

        if (slime.MutationChance <= 0f)
        {
            _popup.PopupPredicted(Loc.GetString("xeno-potion-stabilizer-min", ("name", Name(target))), args.User, args.User);
            return;
        }

        slime.MutationChance = Math.Clamp(slime.MutationChance + SlimeStabilizerPotionComponent.MutationChangeAmount, 0f, 1f);
        _popup.PopupPredicted(Loc.GetString("xeno-potion-stabilizer-applied",
            ("name", Name(target)),
            ("chance", (int) (slime.MutationChance * 100))), args.User, args.User);
        PredictedQueueDel(args.Used);
    }
}
