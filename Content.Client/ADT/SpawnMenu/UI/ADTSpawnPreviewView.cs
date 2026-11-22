using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.ADT.SpawnMenu.UI;

public sealed class ADTSpawnPreviewView : SpriteView
{
    private const float PreviewLifetime = float.MaxValue;
    private const int MaxFastRespawns = 10;

    private static readonly TimeSpan FastRespawnTime = TimeSpan.FromSeconds(0.5);

    private readonly IGameTiming _timing;

    private string? _prototype;
    private EntityUid? _entity;
    private bool _showing;
    private bool _respawnQueued;
    private int _fastRespawns;
    private TimeSpan _spawnedAt;

    public ADTSpawnPreviewView()
    {
        _timing = IoCManager.Resolve<IGameTiming>();
    }

    public void SetPrototype(string? prototype)
    {
        if (prototype == _prototype && _entity != null && EntMan.EntityExists(_entity))
            return;

        _prototype = prototype;
        _fastRespawns = 0;

        if (_showing)
            UpdateEntity();
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _showing = true;
        EntMan.EntityDeleted += OnEntityDeleted;

        if (_prototype != null)
            UpdateEntity();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _showing = false;
        EntMan.EntityDeleted -= OnEntityDeleted;
        DeleteEntity();
    }

    private void OnEntityDeleted(Entity<MetaDataComponent> entity)
    {
        if (entity.Owner != _entity || _respawnQueued)
            return;

        _entity = null;
        SetEntity(null);

        if (_timing.RealTime - _spawnedAt > FastRespawnTime)
            _fastRespawns = 0;

        if (_fastRespawns >= MaxFastRespawns)
            return;

        _fastRespawns++;
        _respawnQueued = true;

        Timer.Spawn(0, () =>
        {
            _respawnQueued = false;

            if (_showing && _entity == null)
                UpdateEntity();
        });
    }

    private void DeleteEntity()
    {
        var entity = _entity;
        _entity = null;
        SetEntity(null);
        EntMan.TryQueueDeleteEntity(entity);
    }

    private void UpdateEntity()
    {
        DeleteEntity();

        if (_prototype == null)
            return;

        var uid = EntMan.Spawn(_prototype);
        if (EntMan.TryGetComponent<TimedDespawnComponent>(uid, out var despawn))
            despawn.Lifetime = PreviewLifetime;

        SpriteSystem ??= EntMan.System<SpriteSystem>();
        SpriteSystem.ForceUpdate(uid);

        _spawnedAt = _timing.RealTime;
        _entity = uid;
        SetEntity(uid);
    }
}
