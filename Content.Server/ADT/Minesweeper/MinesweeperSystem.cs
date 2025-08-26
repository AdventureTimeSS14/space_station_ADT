using Content.Shared.ADT.Minesweeper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.ADT.Minesweeper;

public sealed partial class MinesweeperSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MinesweeperComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIClosedEvent>(OnBoundUiClosed);
        Subs.BuiEvents<MinesweeperComponent>(MinesweeperUiKey.Key, subs =>
        {
            subs.Event<MinesweeperWinMessage>(OnWinMessageReceived);
            subs.Event<MinesweeperLostMessage>(OnLostMessageReceived);
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

            Dirty(ent, comp);
        }

        if (component.SoundWin != null)
            _sharedAudioSystem.PlayPvs(component.SoundWin, uid);
    }

    private void OnLostMessageReceived(EntityUid uid, MinesweeperComponent component, MinesweeperLostMessage msg)
    {
        if (component.SoundLost != null)
            _sharedAudioSystem.PlayPvs(component.SoundLost, uid);

        // TODO: с шансом 1-2% Будет взрыв при проигрыше ^_^
        // if (_random.Next(100) < 2)
        // {
        //     _explosionSystem.ExplodeTile();
        //     // _explosionSystem.QueueExplosion(equipee, "Default", 200f, 10f, 100f, 1f);
        // }
    }
}
