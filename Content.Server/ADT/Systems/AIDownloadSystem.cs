using Content.Shared.Silicons.StationAi;
using Content.Shared.DoAfter;
using Content.Server.Popups;
using Content.Server.Inventory;
using Content.Server.Storage.Components;
using Content.Shared.Mind;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Map;

namespace Content.Server.ADT.Systems;

public sealed class AIDownloadSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private const string ToyPrototype = "ADTToyAI";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiCoreComponent, IntellicardDoAfterEvent>(OnAiDownloadFinished);
    }

    private void OnAiDownloadFinished(Entity<StationAiCoreComponent> core, ref IntellicardDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var user = args.Args.User;
        if (user == null)
            return;

        if (!TryComp<MindComponent>(user.Value, out var mind))
            return;

        var toy = TrySpawnToy(core.Owner, user.Value);
        if (toy == null)
        {
            _popup.PopupEntity("Not enough space to download AI!", core.Owner, user.Value);
            return;
        }

        // Перенос разума игрока в игрушку
        _mind.TransferMind(mind, toy.Value);
    }

    private EntityUid? TrySpawnToy(EntityUid coreUid, EntityUid userUid)
    {
        var coords = Transform(coreUid).Coordinates;

        // проверяем 8 тайлов вокруг ядра
        var offsets = new (int, int)[]
        {
            (-1,-1), (0,-1), (1,-1),
            (-1,0),          (1,0),
            (-1,1),  (0,1),  (1,1)
        };

        foreach (var (ox, oy) in offsets)
        {
            var target = coords.Offset(new Vector2i(ox, oy));
            if (AITileHelper.IsTileFree(EntityManager, target))
            {
                return _entMan.SpawnEntity(ToyPrototype, target);
            }
        }

        // Если тайлов нет – пробуем в руки
        if (_hands.TryPickupAnyHand(userUid, _entMan.SpawnEntity(ToyPrototype, Transform(userUid).Coordinates)))
            return _entMan.SpawnEntity(ToyPrototype, Transform(userUid).Coordinates);

        // Если руки заняты – пробуем в рюкзак
        if (_inventory.TryGetSlotEntity(userUid, "back", out var backpack) &&
            TryComp<StorageComponent>(backpack, out var storage))
        {
            var toy = _entMan.SpawnEntity(ToyPrototype, Transform(userUid).Coordinates);
            if (storage.Insert(toy, userUid))
                return toy;

            QueueDel(toy);
        }

        return null;
    }
}
