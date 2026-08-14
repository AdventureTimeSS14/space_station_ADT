using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Server.Heretic.EntitySystems;

/// <summary>
/// Phantom items created for a heretic flesh mimic can be freely used by the mimic,
/// but are deleted as soon as they end up outside of the mimic's possession.
/// This prevents cloned equipment from being dropped or handed to other players,
/// which would otherwise duplicate items.
/// </summary>
public sealed class HereticCloneItemSystem : EntitySystem
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromSeconds(0.25f);

    private readonly HashSet<EntityUid> _scheduledChecks = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCloneItemComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<HereticCloneItemComponent, ExaminedEvent>(OnExamined);
    }

    private void OnParentChanged(Entity<HereticCloneItemComponent> ent, ref EntParentChangedMessage args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        // Still possessed by the clone (hand, backpack, pockets, etc.) - nothing to do.
        if (IsInsideClone(ent))
            return;

        // The item may just be moving between the clone's own containers (e.g. backpack to hand),
        // so the deletion is deferred briefly until the transfer has settled.
        if (!_scheduledChecks.Add(ent))
            return;

        Timer.Spawn(CheckDelay, () =>
        {
            _scheduledChecks.Remove(ent);

            if (!Exists(ent) || TerminatingOrDeleted(ent))
                return;

            if (!IsInsideClone(ent))
                QueueDel(ent);
        });
    }

    private void OnExamined(Entity<HereticCloneItemComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("heretic-clone-item-examine"));
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
