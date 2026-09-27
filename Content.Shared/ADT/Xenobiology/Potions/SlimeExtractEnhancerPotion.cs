using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that restores one use to an exhausted slime extract.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeExtractEnhancerPotionComponent : Component;

public sealed partial class SlimeExtractEnhancerPotionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeExtractEnhancerPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeExtractEnhancerPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<SlimeExtractComponent>(target, out var extract))
            return;

        args.Handled = true;
        extract.RemainingUses += 1;
        _popup.PopupPredicted(Loc.GetString("xeno-potion-extract-enhancer-applied",
            ("name", Name(target)),
            ("uses", extract.RemainingUses)), args.User, args.User);
        PredictedQueueDel(args.Used);
    }
}
