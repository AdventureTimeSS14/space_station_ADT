using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Shared.ADT.FiringPin;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.ADT.FiringPin;

public sealed class FiringPinAlertLevelSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
        SubscribeLocalEvent<ActorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<ActorComponent>();
        while (query.MoveNext(out var player, out _))
        {
            if (_station.GetOwningStation(player) == args.Station)
                UpdateCache(player, args.AlertLevel);
        }
    }

    private void OnParentChanged(Entity<ActorComponent> ent, ref EntParentChangedMessage args)
    {
        UpdateCacheFromStation(ent.Owner);
    }

    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        UpdateCacheFromStation(args.Entity);
    }

    private void UpdateCacheFromStation(EntityUid player)
    {
        var station = _station.GetOwningStation(player);
        if (station == null || !TryComp<AlertLevelComponent>(station, out var alert))
            return;

        UpdateCache(player, alert.CurrentLevel);
    }

    private void UpdateCache(EntityUid player, string level)
    {
        var cache = EnsureComp<FiringPinAlertLevelCacheComponent>(player);

        if (cache.CurrentLevel == level)
            return;

        cache.CurrentLevel = level;
        Dirty(player, cache);
    }
}