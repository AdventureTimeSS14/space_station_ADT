using Content.Server.Ghost.Roles.Components;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;

namespace Content.Server.ADT.Xenobiology.Potions;

/// <summary>
/// Makes the targeted creature sentient and opens it up as a ghost role.
/// </summary>
public sealed partial class SlimeSentiencePotionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeSentiencePotionComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<SlimeSentiencePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (HasComp<MindContainerComponent>(target))
            return;

        args.Handled = true;
        _mind.MakeSentient(target);

        if (TryComp(target, out GhostRoleComponent? ghostRole))
            return;

        ghostRole = AddComp<GhostRoleComponent>(target);
        EnsureComp<GhostTakeoverAvailableComponent>(target);

        ghostRole.RoleName = Name(target);
        ghostRole.RoleDescription = Loc.GetString("xeno-potion-sentience-ghost-role-description");

        PredictedQueueDel(args.Used);
    }
}
