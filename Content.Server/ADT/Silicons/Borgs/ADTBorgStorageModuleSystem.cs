using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed partial class ADTBorgStorageModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedBorgSystem _borg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<ADTBorgStorageInsertHeldRequestEvent>(OnInsertHeld);
        SubscribeLocalEvent<ADTBorgStorageModuleComponent, EntGotRemovedFromContainerMessage>(OnModuleRemoved);
        SubscribeLocalEvent<ADTBorgStorageModuleComponent, BorgModuleInsertAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertHeld(ADTBorgStorageInsertHeldRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player ||
            !TryComp<BorgChassisComponent>(player, out var chassis) ||
            !TryComp<HandsComponent>(player, out var hands))
            return;

        if (!_hands.TryGetActiveItem((player, hands), out var held))
            return;

        if (!_borg.TryGetStorageModule((player, chassis), out var module))
            return;

        if (_storage.Insert(module.Value, held.Value, out _, out var reason, player))
            return;

        if (reason != null)
            _popup.PopupEntity(reason, module.Value, player);
    }

    private void OnModuleRemoved(Entity<ADTBorgStorageModuleComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<BorgChassisComponent>(args.Container.Owner, out var chassis) ||
            args.Container.ID != chassis.ModuleContainerId)
            return;

        if (!_container.TryGetContainer(ent.Owner, StorageComponent.ContainerId, out var storage))
            return;

        for (var i = storage.ContainedEntities.Count - 1; i >= 0; i--)
        {
            _transform.DropNextTo(storage.ContainedEntities[i], ent.Owner);
        }
    }

    private void OnInsertAttempt(Entity<ADTBorgStorageModuleComponent> ent, ref BorgModuleInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<BorgChassisComponent>(args.ChassisEnt, out var chassis))
            return;

        foreach (var module in chassis.ModuleContainer.ContainedEntities)
        {
            if (module != ent.Owner && HasComp<ADTBorgStorageModuleComponent>(module))
            {
                args.Cancelled = true;
                args.Reason = Loc.GetString("adt-borg-storage-duplicate");
                return;
            }
        }
    }
}
