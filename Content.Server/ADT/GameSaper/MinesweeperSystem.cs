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
        // _userName = name.EntityName;
        // component.LastOpenedBy = name.EntityName;
        // Dirty(uid, component);
    }

    private void OnBoundUiClosed(EntityUid uid, MinesweeperComponent component, BoundUIClosedEvent args)
    {
        var actor = args.Actor;
        var name = _entityManager.GetComponent<MetaDataComponent>(actor);
        Logger.Warning($"Игрок {name} ЗАКРЫЛ сапёра на объекте {ToPrettyString(uid)} name {name.EntityName}");
        // component.LastOpenedBy = null;
        // Dirty(uid, component);
    }
}
