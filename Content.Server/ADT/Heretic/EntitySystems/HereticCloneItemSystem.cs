using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Server.Heretic.EntitySystems;

/// <summary>
/// Phantom items created for a heretic flesh mimic can be freely used by the mimic,
/// but are deleted as soon as they end up outside of the mimic's possession.
/// This prevents cloned equipment from being dropped or handed to other players,
/// which would otherwise duplicate items.
/// </summary>
public sealed class HereticCloneItemSystem : EntitySystem
{
    private readonly HashSet<EntityUid> _pendingChecks = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCloneItemComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
        SubscribeLocalEvent<HereticCloneItemComponent, ExaminedEvent>(OnExamined);
    }

    private void OnRemovedFromContainer(Entity<HereticCloneItemComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // The item may just be moving between the clone's own containers (e.g. backpack to hand),
        // so the deletion is deferred until the transfer has settled.
        _pendingChecks.Add(ent);
    }

    private void OnExamined(Entity<HereticCloneItemComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("heretic-clone-item-examine"));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pendingChecks.Count == 0)
            return;

        foreach (var item in _pendingChecks)
        {
            if (!Exists(item) || TerminatingOrDeleted(item))
                continue;

            if (!IsInsideClone(item))
                QueueDel(item);
        }

        _pendingChecks.Clear();
    }

    private bool IsInsideClone(EntityUid item)
    {
        var parent = Transform(item).ParentUid;
        while (parent.IsValid())
        {
            if (HasComp<HereticMinionComponent>(parent))
                return true;

            parent = Transform(parent).ParentUid;
        }

        return false;
    }
}
