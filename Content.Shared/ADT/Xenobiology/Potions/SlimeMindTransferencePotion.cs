using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that transfers the user's mind into a mindless living creature.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeMindTransferencePotionComponent : Component;

public sealed partial class SlimeMindTransferencePotionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeMindTransferencePotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeMindTransferencePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        // The user must have a mind and the target must be able to possess one, but not have one already.
        if (!TryComp<MindContainerComponent>(args.User, out var userMindContainer))
            return;

        if (!TryComp<MindContainerComponent>(target, out var targetMindContainer))
            return;

        if (userMindContainer.Mind is not { } mind)
            return;

        if (targetMindContainer.HasMind)
            return;

        args.Handled = true;
        _mind.TransferTo(mind, target);
        PredictedQueueDel(args.Used);
    }
}
