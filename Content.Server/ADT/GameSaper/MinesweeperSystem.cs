using Content.Shared.ADT.Minesweeper;

namespace Content.Server.ADT.Minesweeper;

public sealed partial class MinesweeperSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MinesweeperComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIClosedEvent>(OnBoundUiClosed);
        Subs.BuiEvents<MinesweeperComponent>(MinesweeperUiKey.Key, subs =>
        {
            subs.Event<MinesweeperWinMessage>(OnWinMessageReceived);
        });
    }

    private void OnBoundUiOpened(EntityUid uid, MinesweeperComponent component, BoundUIOpenedEvent args)
    {
        var actor = args.Actor;
        if (_entityManager.TryGetComponent<MetaDataComponent>(actor, out var meta))
        {
            component.LastOpenedBy = meta.EntityName;
            Dirty(uid, component);
        }
    }

    private void OnBoundUiClosed(EntityUid uid, MinesweeperComponent component, BoundUIClosedEvent args)
    {
        component.LastOpenedBy = null;
        Dirty(uid, component);
    }

    private void OnWinMessageReceived(EntityUid uid, MinesweeperComponent component, MinesweeperWinMessage msg)
    {
        // Проходим по всем сущностям с компонентом MinesweeperComponent
        // И при выйгрыше на одном из автоматов, передаём рекорд во все компоненты таких сущностей
        var query = _entityManager.EntityQueryEnumerator<MinesweeperComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            comp.Records.Add(new MinesweeperRecord
            {
                Difficulty = msg.Difficulty,
                TimeSeconds = msg.TimeSeconds,
                EntityName = msg.NameWin
            });

            _entityManager.Dirty(ent, comp);
        }
    }
}
