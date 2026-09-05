using System.Linq;
using System.Numerics;
using Content.Server.Power.EntitySystems;
using Content.Server.VendingMachines;
using Content.Shared.ADT.VendingMachines;
using Content.Shared.Clothing.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Objectives.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Content.Shared.Verbs;
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
    [Dependency] private readonly VendingMachineSystem _vending = default!;

    private const string ReturnedItemsContainerId = "ADTVendingReturnedItems";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VendingMachineComponent, ADTVendingReturnedEjectEvent>(OnReturnedEject);
        SubscribeLocalEvent<VendingMachineComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public bool TryReturnItem(EntityUid uid, VendingMachineComponent component, EntityUid user, EntityUid used)
    {
        if (HasComp<StealTargetComponent>(used) || ContainsStealTarget(used))
        {
            Deny(uid, component);
            return false;
        }

        var protoId = MetaData(used).EntityPrototype?.ID;
        if (string.IsNullOrEmpty(protoId) || !component.Inventory.ContainsKey(protoId))
        {
            Deny(uid, component);
            return false;
        }

        var container = _container.EnsureContainer<Container>(uid, ReturnedItemsContainerId);
        if (!_container.Insert(used, container))
        {
            Deny(uid, component);
            return false;
        }

        component.ReturnedInventory[protoId] = component.ReturnedInventory.GetValueOrDefault(protoId) + 1;
        Dirty(uid, component);

        _popup.PopupEntity(
            Loc.GetString("vending-machine-return-success", ("item", Identity.Entity(used, EntityManager))),
            uid, user);
        _audio.PlayPvs(component.SoundInsertCurrency, uid);
        return true;
    }

    private void OnGetVerbs(EntityUid uid, VendingMachineComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Using is not { } used)
            return;

        if (component.Broken || !this.IsPowered(uid, EntityManager))
            return;

        var protoId = MetaData(used).EntityPrototype?.ID;
        if (string.IsNullOrEmpty(protoId) || !component.Inventory.ContainsKey(protoId))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("vending-machine-return-verb"),
            Category = VerbCategory.Insert,
            Act = () => TryReturnItem(uid, component, args.User, used),
        };
        args.Verbs.Add(verb);
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

            PaintClothing(returned, args.PaintColor);

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
        _vending.Deny(uid, component);
    }

    public void PaintClothing(EntityUid uid, Color? color)
    {
        if (!HasComp<ClothingComponent>(uid))
            return;

        if (color is { } paintColor)
        {
            var paint = EnsureComp<ADTClothingPaintComponent>(uid);
            paint.PaintColor = paintColor;
            Dirty(uid, paint);
        }
        else if (TryComp<ADTClothingPaintComponent>(uid, out var paint))
        {
            paint.PaintColor = null;
            Dirty(uid, paint);
        }
    }
}