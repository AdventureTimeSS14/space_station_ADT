using Content.Shared.ADT.Minesweeper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Shared.Emag.Systems;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server.ADT.Minesweeper;

public sealed partial class MinesweeperSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly EmagSystem _emagSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MinesweeperEmagComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIClosedEvent>(OnBoundUiClosed);
        Subs.BuiEvents<MinesweeperComponent>(MinesweeperUiKey.Key, subs =>
        {
            subs.Event<MinesweeperWinMessage>(OnWinMessageReceived);
            subs.Event<MinesweeperLostMessage>(OnLostMessageReceived);
        });
    }

    private void OnEmagged(EntityUid uid, MinesweeperEmagComponent component, ref GotEmaggedEvent args)
    {
        Logger.Warning($"{ToPrettyString(uid)} ЕМАГНУЛИ");

        if (!TryComp<MinesweeperComponent>(uid, out var minesweeperComponent))
            return;

        minesweeperComponent.IsEmagged = true;
        args.Handled = true;
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

        if (component.IsEmagged)
        {
            _explosionSystem.QueueExplosion(uid, "Cryo", 200f, 10f, 100f, 1f); // Да я это захардкодю, вопросы?
            return;
        }

        // с шансом 10% Будет взрыв при проигрыше ^_^
        if (_random.Next(100) < 10)
        {
            _explosionSystem.QueueExplosion(
                uid,
                "Default",
                totalIntensity: 0.01f,
                slope: 1f,
                maxTileIntensity: 0f,
                tileBreakScale: 0f,
                maxTileBreak: 0,
                canCreateVacuum: false,
                user: null,
                addLog: false
            );
        }
    }
}
