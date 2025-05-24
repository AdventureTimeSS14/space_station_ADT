// TODO: Запись рекордов. Работа сервера и клиента сапёра

// using Content.Shared.ADT.Minesweeper;

// namespace Content.Server.ADT.Minesweeper;

// public sealed class MinesweeperSystem : EntitySystem
// {
//     [Dependency] private readonly IEntityManager _entityManager = default!;
//     public override void Initialize()
//     {
//         base.Initialize();
//         SubscribeLocalEvent<MinesweeperComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
//         SubscribeNetworkEvent<MinesweeperWinMessage>(OnWinMessageReceived);
//     }

//     private void OnBoundUiOpened(EntityUid uid, MinesweeperComponent component, BoundUIOpenedEvent args)
//     {
//         // args.Session — это игрок, открывший интерфейс
//         var actor = args.Actor;
//         var name = _entityManager.GetComponent<MetaDataComponent>(actor);

//         Logger.Info($"Игрок {name} открыл сапёра на объекте {ToPrettyString(uid)} name {name} namee {name.EntityName}");

//         // Пример: сохрани имя в компонент, если нужно
//         component.LastOpenedBy = name.EntityName;
//     }

//     private void OnWinMessageReceived(MinesweeperWinMessage msg, EntitySessionEventArgs args)
//     {
//         if (args.SenderSession.AttachedEntity is not { } uid)
//             return;

//         if (!_entityManager.TryGetComponent<MetaDataComponent>(uid, out var meta))
//             return;

//         if (!_entityManager.TryGetComponent<MinesweeperComponent>(uid, out var comp))
//             return;

//         var name = meta.EntityName;

//         comp.Records.Add(new MinesweeperRecord
//         {
//             Difficulty = msg.Difficulty,
//             TimeSeconds = msg.TimeSeconds,
//             EntityName = name
//         });

//         _entityManager.Dirty(uid, comp);
//     }
// }
