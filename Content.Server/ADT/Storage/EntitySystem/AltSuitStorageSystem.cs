using Content.Server.ADT.SuitStorage;
using Content.Shared.ADT.SuitStorage;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.SuitStorage;

public sealed class SuitStorageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SuitStorageUnitComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<SuitStorageUnitComponent, SuitStorageGetItemMessage>(OnGetItem);
    }

    private void OnUiOpened(EntityUid uid, SuitStorageUnitComponent comp, BoundUIOpenedEvent args)
    {
        // TODO: отправить UI состояние (какие предметы уже выданы)
    }

    private void OnGetItem(EntityUid uid, SuitStorageUnitComponent comp, SuitStorageGetItemMessage args)
    {
        var user = args.Session.AttachedEntity;
        if (user == null)
            return;

        string? proto = args.Slot switch
        {
            "Suit" => comp.SuitTaken ? null : comp.SuitPrototype,
            "Boots" => comp.BootsTaken ? null : comp.BootsPrototype,
            "Breathmask" => comp.MaskTaken ? null : comp.BreathMaskPrototype,
            "Storage" => comp.StorageTaken ? null : comp.StoragePrototype,
            _ => null
        };

        if (proto == null || !_proto.HasIndex<EntityPrototype>(proto))
            return;

        var ent = EntityManager.SpawnEntity(proto, Transform(user.Value).Coordinates);
        // Можно попробовать в руку
        // hands.TryPickup(user.Value, ent);

        // отметить как "взято"
        switch (args.Slot)
        {
            case "Suit": comp.SuitTaken = true; break;
            case "Boots": comp.BootsTaken = true; break;
            case "Breathmask": comp.MaskTaken = true; break;
            case "Storage": comp.StorageTaken = true; break;
        }

        Dirty(uid, comp);
    }
}