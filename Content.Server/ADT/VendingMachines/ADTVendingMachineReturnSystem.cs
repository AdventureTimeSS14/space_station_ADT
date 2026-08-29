using System.Linq;
using System.Numerics;
using Content.Server.VendingMachines;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.ADT.VendingMachines;

public sealed class ADTVendingMachineReturnSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwingSystem = default!;

    private const string ReturnedItemsContainerId = "ADTVendingReturnedItems";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, ADTVendingReturnedEjectEvent>(OnReturnedEject);
    }

    public void TryReturnItem(EntityUid uid, VendingMachineComponent component, InteractUsingEvent args)
    {
        if (HasComp<StealTargetComponent>(args.Used) || ContainsStealTarget(args.Used))
        {
            Deny(uid, component);
            return;
        }

        var protoId = MetaData(args.Used).EntityPrototype?.ID;
        if (string.IsNullOrEmpty(protoId) || !component.Inventory.ContainsKey(protoId))
        {
            Deny(uid, component);
            return;
        }

        var container = _container.EnsureContainer<Container>(uid, ReturnedItemsContainerId);
        if (!_container.Insert(args.Used, container))
        {
            Deny(uid, component);
            return;
        }

        component.ReturnedInventory[protoId] = component.ReturnedInventory.GetValueOrDefault(protoId) + 1;

        args.Handled = true;
        Dirty(uid, component);
        _popup.PopupEntity(
            Loc.GetString("vending-machine-return-success", ("item", Identity.Entity(args.Used, EntityManager))),
            uid, args.User);
        _audio.PlayPvs(component.SoundInsertCurrency, uid);
    }
    private bool ContainsStealTarget(EntityUid item)
    {
        if (!TryComp<StorageComponent>(item, out var storage) || storage.Container == null)
            return false;

        return storage.Container.ContainedEntities.Any(e => HasComp<StealTargetComponent>(e));
    }

    private void OnReturnedEject(EntityUid uid, VendingMachineComponent component, ADTVendingReturnedEjectEvent args)
    {
        var container = _container.EnsureContainer<Container>(uid, ReturnedItemsContainerId);

        for (var i = 0; i < args.Count; i++)
        {
            var returned = container.ContainedEntities.FirstOrDefault(e =>
                TryComp<MetaDataComponent>(e, out var meta) &&
                meta.EntityPrototype?.ID == args.ItemProtoId);

            if (!Exists(returned))
                break;

            _container.Remove(returned, container, force: true, destination: args.Coordinates);

            if (args.ThrowItem)
            {
                var range = component.NonLimitedEjectRange;
                var direction = new Vector2(_random.NextFloat(-range, range), _random.NextFloat(-range, range));
                _throwingSystem.TryThrow(returned, direction, component.NonLimitedEjectForce);
            }
        }
    }

    private void Deny(EntityUid uid, VendingMachineComponent component)
    {
        EntityManager.System<VendingMachineSystem>().Deny(uid, component);
    }
}