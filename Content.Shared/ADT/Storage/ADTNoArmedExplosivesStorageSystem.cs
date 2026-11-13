using Content.Shared.Explosion.Components;
using Content.Shared.Trigger.Components;
using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Storage;

public sealed class ADTNoArmedExplosivesStorageSystem : EntitySystem
{
    private const int MaxDepth = 8;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTNoArmedExplosivesStorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(Entity<ADTNoArmedExplosivesStorageComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container.ID != ent.Comp.ContainerId)
            return;

        if (ContainsArmedExplosive(args.EntityUid, 0))
            args.Cancel();
    }

    private bool ContainsArmedExplosive(EntityUid uid, int depth)
    {
        if (IsArmedExplosive(uid))
            return true;

        if (depth >= MaxDepth || !TryComp<ContainerManagerComponent>(uid, out var manager))
            return false;

        foreach (var container in manager.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (ContainsArmedExplosive(contained, depth + 1))
                    return true;
            }
        }

        return false;
    }

    private bool IsArmedExplosive(EntityUid uid)
    {
        if (!HasComp<ExplosiveComponent>(uid))
            return false;

        return HasComp<ActiveTimerTriggerComponent>(uid) || HasComp<TriggerOnSignalComponent>(uid);
    }
}
