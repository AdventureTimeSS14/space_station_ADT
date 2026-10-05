using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that makes a slime produce extra extracts when processed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeSteroidPotionComponent : Component;

public sealed partial class SlimeSteroidPotionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeSteroidPotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeSteroidPotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<SlimeComponent>(target, out var slime))
            return;

        args.Handled = true;
        slime.SlimeSteroidAmount += 1;
        _popup.PopupPredicted(Loc.GetString("xeno-potion-steroid-applied",
            ("name", Name(target)),
            ("amount", slime.SlimeSteroidAmount)), args.User, args.User);
        PredictedQueueDel(args.Used);
    }
}
