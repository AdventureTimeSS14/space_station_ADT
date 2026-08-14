using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.Heretic.EntitySystems;

/// <summary>
/// Phantom items created for a heretic flesh mimic can be freely used by the mimic,
/// but never leave its possession: the mimic cannot drop or throw them, nobody can
/// unequip or strip them, and they are deleted when the mimic dies.
/// This prevents cloned equipment from being duplicated.
/// </summary>
public sealed class HereticCloneItemSystem : EntitySystem
{
    private static readonly TimeSpan CheckDelay = TimeSpan.FromSeconds(0.25f);

    private readonly HashSet<EntityUid> _scheduledChecks = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticCloneItemComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<HereticMinionComponent, IsUnequippingTargetAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<HereticMinionComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<HereticCloneItemComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<HereticCloneItemComponent, ExaminedEvent>(OnExamined);
    }

    // The mimic may use clone items, but never drop or throw them.
    // Stripping from hands also routes through TryDrop, so this blocks it too.
    private void OnDropAttempt(Entity<HereticCloneItemComponent> ent, ref DropAttemptEvent args)
    {
        if (!HasComp<HereticMinionComponent>(args.Uid))
            return;

        args.Cancel();
    }

    // Nobody - including the mimic itself - may take clone items off its body.
    private void OnUnequipAttempt(Entity<HereticMinionComponent> ent, ref IsUnequippingTargetAttemptEvent args)
    {
        if (!HasComp<HereticCloneItemComponent>(args.Equipment))
            return;

        args.Cancel();
    }

    private void OnMobStateChanged(Entity<HereticMinionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        DeleteCloneItems(ent);
    }

    private void OnParentChanged(Entity<HereticCloneItemComponent> ent, ref EntParentChangedMessage args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        // Still possessed by the clone (hand, backpack, pockets, etc.) - nothing to do.
        if (IsInsideClone(ent))
            return;

        // Safety net for anything that slipped past the drop/unequip blocks
        // (e.g. taken out of the clone's bag): delete once the transfer has settled.
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

    private void DeleteCloneItems(EntityUid uid)
    {
        var toDelete = new List<EntityUid>();
        CollectCloneItems(uid, toDelete);

        foreach (var item in toDelete)
        {
            QueueDel(item);
        }
    }

    private void CollectCloneItems(EntityUid uid, List<EntityUid> toDelete)
    {
        if (!TryComp<ContainerManagerComponent>(uid, out var manager))
            return;

        foreach (var container in manager.Containers.Values)
        {
            foreach (var item in container.ContainedEntities)
            {
                if (!HasComp<HereticCloneItemComponent>(item))
                    continue;

                CollectCloneItems(item, toDelete);
                toDelete.Add(item);
            }
        }
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
