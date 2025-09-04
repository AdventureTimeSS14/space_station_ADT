using Content.Server.Explosion.EntitySystems;
using Content.Shared.ADT.Minesweeper;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

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
        SubscribeLocalEvent<MinesweeperComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<MinesweeperComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIClosedEvent>(OnBoundUiClosed);
        Subs.BuiEvents<MinesweeperComponent>(MinesweeperUiKey.Key, subs =>
        {
            subs.Event<MinesweeperWinMessage>(OnWinMessageReceived);
            subs.Event<MinesweeperLostMessage>(OnLostMessageReceived);
        });
    }

    private void OnComponentInit(EntityUid uid, MinesweeperComponent component, ComponentInit args)
    {
        // Random amount of prizes
        component.RewardAmount = _random.Next(component.RewardMinAmount, component.RewardMaxAmount + 1);
    }

    private void OnEmagged(EntityUid uid, MinesweeperComponent component, ref GotEmaggedEvent args)
    {
        if (!_emagSystem.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emagSystem.CheckFlag(uid, EmagType.Interaction))
            return;

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

        ProcessWin(uid, component);

        if (component.SoundWin != null)
            _sharedAudioSystem.PlayPvs(component.SoundWin, uid);
    }

    private void ProcessWin(EntityUid uid, MinesweeperComponent? arcade = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref arcade, ref xform))
            return;
        if (arcade.RewardAmount <= 0)
            return;

        EntityManager.SpawnEntity(_random.Pick(arcade.PossibleRewards), xform.Coordinates);
        arcade.RewardAmount--;
    }

    private void OnLostMessageReceived(EntityUid uid, MinesweeperComponent component, MinesweeperLostMessage msg)
    {
        if (component.SoundLost != null)
            _sharedAudioSystem.PlayPvs(component.SoundLost, uid);

        if (_emagSystem.CheckFlag(uid, EmagType.Interaction))
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
