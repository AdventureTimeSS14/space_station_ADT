using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Containers;

/// <summary>
/// System for getting container that is linked to subject entity. Container is supposed to be present in certain character slot.
/// Can be used for linking ammo feeder, solution source for spray nozzle, etc.
/// </summary>
public sealed partial class SlotBasedConnectedContainerSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!; // ADT-Tweak 

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<SlotBasedConnectedContainerComponent, GetConnectedContainerEvent>(OnGettingConnectedContainer);
    }

    /// <summary>
    /// Try get connected container entity in character slots for <see cref="uid"/>.
    /// </summary>
    /// <param name="uid">
    /// Entity for which connected container is required. If <see cref="SlotBasedConnectedContainerComponent"/>
    /// is used - tries to find container in slot, returns false and null <see cref="slotEntity"/> otherwise.
    /// </param>
    /// <param name="slotEntity">Found connected container entity or null.</param>
    /// <returns>True if connected container was found, false otherwise.</returns>
    public bool TryGetConnectedContainer(EntityUid uid, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        if (!TryComp<SlotBasedConnectedContainerComponent>(uid, out var component))
        {
            slotEntity = null;
            return false;
        }

        return TryFindConnectedContainer(uid, component, out slotEntity); // ADT-Tweak 
    }

    private void OnGettingConnectedContainer(Entity<SlotBasedConnectedContainerComponent> ent, ref GetConnectedContainerEvent args)
    {
        if (TryFindConnectedContainer(ent, ent.Comp, out var val)) // ADT-Tweak 
            args.ContainerEntity = val;
    }

    private bool TryFindConnectedContainer(EntityUid uid, SlotBasedConnectedContainerComponent component, [NotNullWhen(true)] out EntityUid? slotEntity) // ADT-Tweak 
    {
        slotEntity = null;

        if (!_containers.TryGetContainingContainer((uid, null, null), out var container))
            return false;

        var user = container.Owner;
        if (!_inventory.TryGetContainerSlotEnumerator(user, out var enumerator, component.TargetSlot)) // ADT-Tweak 
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (_whitelistSystem.IsWhitelistFailOrNull(component.ContainerWhitelist, item)) // ADT-Tweak 
                continue;

            slotEntity = item;
            return true;
        }

        // ADT-Tweak start
        if (component.CheckHands)
        {
            foreach (var item in _hands.EnumerateHeld((user, null)))
            {
                if (_whitelistSystem.IsWhitelistFailOrNull(component.ContainerWhitelist, item))
                    continue;

                slotEntity = item;
                return true;
            }
        }
        // ADT-Tweak end

        return false;
    }
}

/// <summary>
/// Event for an attempt of getting container, connected to entity on which event was raised.
/// Fills <see cref="ContainerEntity"/> if connected container exists.
/// </summary>
[ByRefEvent]
public struct GetConnectedContainerEvent
{
    /// <summary>
    /// Container entity, if it exists, or null.
    /// </summary>
    public EntityUid? ContainerEntity;
}
