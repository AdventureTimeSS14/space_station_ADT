using Content.Server.Popups;
using Content.Shared.ADT.Minesweeper;

namespace Content.Server.ADT.Minesweeper;

public sealed partial class MinesweeperSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    private string _userName = "";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MinesweeperComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIClosedEvent>(OnBoundUiClosed);
        // SubscribeNetworkEvent<MinesweeperWinMessage>(OnWinMessageReceived);
        Subs.BuiEvents<MinesweeperComponent>(MinesweeperUiKey.Key, subs =>
        {
            subs.Event<MinesweeperWinMessage>(OnWinMessageReceived);
        });
    }

    private void OnBoundUiOpened(EntityUid uid, MinesweeperComponent component, BoundUIOpenedEvent args)
    {
        // args.Session — это игрок, открывший интерфейс
        var actor = args.Actor;
        var name = _entityManager.GetComponent<MetaDataComponent>(actor);

        // if (component.LastOpenedBy != null)
        // {
        //     var message = Loc.GetString("machine-already-in-use", ("machine", uid));
        //     _popupSystem.PopupEntity(message, uid, args.Entity);
        //     Logger.Warning($"{name.EntityName} Попытался открыть используемый {ToPrettyString(uid)} ");
        //     return;
        // }

        Logger.Warning($"Игрок {name} открыл сапёра на объекте {ToPrettyString(uid)} name {name.EntityName}");
        _userName = name.EntityName;
        component.LastOpenedBy = name.EntityName;
        Dirty(uid, component);
    }

    private void OnBoundUiClosed(EntityUid uid, MinesweeperComponent component, BoundUIClosedEvent args)
    {
        var actor = args.Actor;
        var name = _entityManager.GetComponent<MetaDataComponent>(actor);
        Logger.Warning($"Игрок {name} ЗАКРЫЛ сапёра на объекте {ToPrettyString(uid)} name {name.EntityName}");
        component.LastOpenedBy = null;
        Dirty(uid, component);
    }

    private void OnWinMessageReceived(EntityUid uid, MinesweeperComponent comp, MinesweeperWinMessage msg)
    {
        Logger.Warning($"ПРИШЛО OnWinMessageReceived");
        if (!_entityManager.TryGetComponent<MetaDataComponent>(uid, out var meta))
            return;

        var name = meta.EntityName;
        Logger.Warning($"ПРИШЛО OnWinMessageReceived {name}");

        // Добавляем новый рекорд
        comp.Records.Add(new MinesweeperRecord
        {
            Difficulty = msg.Difficulty,
            TimeSeconds = msg.TimeSeconds,
            EntityName = msg.NameWin
        });

        // Синхронизируем компонент с клиентом
        _entityManager.Dirty(uid, comp);
    }
}
