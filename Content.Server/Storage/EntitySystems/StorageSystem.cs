using Content.Server.Administration.Managers;
using Content.Server.Chemistry.Components;
using Content.Shared.Administration;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Explosion;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Lock;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem : SharedStorageSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    // ADT-TWeak Start
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    // ADT-TWeak End

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StorageComponent, BeforeExplodeEvent>(OnExploded);

        SubscribeLocalEvent<StorageFillComponent, MapInitEvent>(OnStorageFillMapInit);
    }

    private void OnExploded(Entity<StorageComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.Container.ContainedEntities);
    }

    /// <inheritdoc />
    public override void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates, EntityCoordinates finalCoordinates,
        Angle initialRotation, EntityUid? user = null)
    {
        var filter = Filter.Pvs(uid).RemoveWhereAttachedEntity(e => e == user);
        RaiseNetworkEvent(new PickupAnimationEvent(GetNetEntity(uid), GetNetCoordinates(initialCoordinates), GetNetCoordinates(finalCoordinates), initialRotation), filter);
    }

    // ADT-TWeak Start: Transfer from bottle-packages
    protected override void AddTransferVerbs(EntityUid uid, StorageComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        base.AddTransferVerbs(uid, component, args);

        // if the target is ChemMaster, add a verb to transfer bottles.
        if (TryComp(args.Target, out ChemMasterComponent? targetChemMaster))
        {
            UtilityVerb verb = new()
            {
                Text = Loc.GetString("storage-component-transfer-verb"),
                IconEntity = GetNetEntity(args.Using),
                Act = () => TransferBottlesToChemMaster(uid, args.Target, args.User, component, targetChemMaster)
            };

            args.Verbs.Add(verb);
        }
    }

    private void TransferBottlesToChemMaster(EntityUid source, EntityUid target, EntityUid? user, StorageComponent sourceComp, ChemMasterComponent targetComp)
    {
        var entities = sourceComp.Container.ContainedEntities.ToArray();
        foreach (var entity in entities)
        {
            if (!_tag.HasTag(entity, "Bottle"))
                continue;

            TryInsertBottleIntoChemMaster(entity, target, user, targetComp);
        }
    }

    private bool TryInsertBottleIntoChemMaster(EntityUid entity, EntityUid target, EntityUid? user, ChemMasterComponent targetComp)
    {
        for (uint slotIndex = 0; slotIndex < targetComp.MaxBottles; slotIndex++)
        {
            var slotId = $"bottleSlot{slotIndex}";

            if (_itemSlotsSystem.TryGetSlot(target, slotId, out var slot) && !slot.HasItem)
            {
                if (_itemSlotsSystem.TryInsert(target, slotId, entity, user))
                    return true;
            }
        }

        return false;
    }
    // ADT-TWeak End
}
