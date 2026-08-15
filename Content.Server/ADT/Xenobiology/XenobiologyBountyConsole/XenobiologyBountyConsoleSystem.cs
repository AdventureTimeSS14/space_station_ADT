using System.Linq;
using Content.Shared.ADT.Xenobiology.XenobiologyBountyConsole;
using Content.Server.Research.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology.XenobiologyBountyConsole;

/// <summary>
/// This handles the Xenobiology console.
/// </summary>
public sealed class XenobiologyBountyConsoleSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly StationXenobiologyBountyDatabaseSystem _xenoDatabase = default!;

    private EntityQuery<StackComponent> _stackQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyBountyConsoleComponent, BoundUIOpenedEvent>(OnBountyConsoleOpened);
        SubscribeLocalEvent<XenobiologyBountyConsoleComponent, BountyFulfillMessage>(OnFulfillMessage);
        SubscribeLocalEvent<XenobiologyBountyConsoleComponent, BountySkipMessage>(OnSkipBountyMessage);

        _stackQuery = GetEntityQuery<StackComponent>();
    }

    private void OnBountyConsoleOpened(Entity<XenobiologyBountyConsoleComponent> console, ref BoundUIOpenedEvent args)
    {
        if (_station.GetOwningStation(console) is not { } station ||
            !TryComp<StationXenobiologyBountyDatabaseComponent>(station, out var db))
            return;

        UpdateState(console, db);
    }

    private void OnFulfillMessage(Entity<XenobiologyBountyConsoleComponent> console, ref BountyFulfillMessage args)
    {
        if (_station.GetOwningStation(console) is not { } station
            || !_xenoDatabase.TryGetBountyFromId(station, args.BountyId, out var bounty)
            || !TryComp<StationXenobiologyBountyDatabaseComponent>(station, out var db))
            return;

        if (!IsBountyComplete(args.Actor, bounty, out var bountyEntities))
        {
            if (_timing.CurTime >= console.Comp.NextDenySoundTime)
            {
                console.Comp.NextDenySoundTime = _timing.CurTime + console.Comp.DenySoundDelay;
                _audio.PlayPvs(console.Comp.DenySound, console);
            }

            return;
        }

        if (!_proto.TryIndex(bounty.Bounty, out var bountyProto)
            || bountyProto.PointsAwarded <= 0
            || !_research.TryGetClientServer(console, out var server, out var serverComponent))
            return;

        foreach (var bountyEnt in bountyEntities)
            Del(bountyEnt);

        _research.ModifyServerPoints(server.Value, (int) bountyProto.PointsAwarded, serverComponent);
        _xenoDatabase.TryRemoveBounty(station, bounty, false, args.Actor);
        _xenoDatabase.FillBountyDatabase(station);

        _audio.PlayPvs(console.Comp.FulfillSound, console);

        UpdateState(console, db);
    }

    private void OnSkipBountyMessage(Entity<XenobiologyBountyConsoleComponent> console, ref BountySkipMessage args)
    {
        if (_station.GetOwningStation(console) is not { } station
            || !TryComp<StationXenobiologyBountyDatabaseComponent>(station, out var db)
            || _timing.CurTime < db.NextSkipTime
            || !_xenoDatabase.TryGetBountyFromId(station, args.BountyId, out var bounty)
            || args.Actor is not { Valid: true } mob)
            return;

        if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent) &&
            !_access.IsAllowed(mob, console, accessReaderComponent))
        {
            if (_timing.CurTime >= console.Comp.NextDenySoundTime)
            {
                console.Comp.NextDenySoundTime = _timing.CurTime + console.Comp.DenySoundDelay;
                _audio.PlayPvs(console.Comp.DenySound, console);
            }

            return;
        }

        if (!_xenoDatabase.TryRemoveBounty(station, bounty, true, args.Actor))
            return;

        _xenoDatabase.FillBountyDatabase(station);
        db.NextSkipTime = _timing.CurTime + db.SkipDelay;
        UpdateState(console, db);
    }

    #region Bounty Management

    private bool IsValidBountyEntry(EntityUid entity, XenobiologyBountyItemEntry entry)
    {
        return _whitelist.IsValid(entry.Whitelist, entity)
            && (entry.Blacklist == null || !_whitelist.IsValid(entry.Blacklist, entity));
    }

    private bool IsBountyComplete(EntityUid entity, XenobiologyBountyData data, out HashSet<EntityUid> bountyEntities)
    {
        bountyEntities = [];

        if (!_proto.TryIndex(data.Bounty, out var proto))
            return false;

        return IsBountyComplete(GetBountyEntities(entity), proto.Entries, out bountyEntities);
    }

    public bool IsBountyComplete(EntityUid entity, ProtoId<XenobiologyBountyPrototype> prototypeId)
    {
        return _proto.TryIndex(prototypeId, out var prototype)
            && IsBountyComplete(GetBountyEntities(entity), prototype.Entries, out _);
    }

    private bool IsBountyComplete(EntityUid container, IEnumerable<XenobiologyBountyItemEntry> entries)
    {
        return IsBountyComplete(GetBountyEntities(container), entries, out _);
    }

    private bool IsBountyComplete(HashSet<EntityUid> entities, IEnumerable<XenobiologyBountyItemEntry> entries, out HashSet<EntityUid> bountyEntities)
    {
        bountyEntities = [];

        foreach (var entry in entries)
        {
            var count = 0;

            var temp = new HashSet<EntityUid>();
            foreach (var entity in entities.Where(entity => IsValidBountyEntry(entity, entry)))
            {
                count += _stackQuery.CompOrNull(entity)?.Count ?? 1;
                temp.Add(entity);

                if (count >= entry.Amount)
                    break;
            }

            if (count < entry.Amount)
                return false;

            foreach (var ent in temp)
            {
                entities.Remove(ent);
                bountyEntities.Add(ent);
            }
        }

        return true;
    }

    private HashSet<EntityUid> GetBountyEntities(EntityUid uid)
    {
        var entities = new HashSet<EntityUid> { uid };

        if (!TryComp<ContainerManagerComponent>(uid, out var containers))
            return entities;

        foreach (var child in containers.Containers.Values
                     .SelectMany(container => container.ContainedEntities, (_, ent) => GetBountyEntities(ent))
                     .SelectMany(children => children))
            entities.Add(child);

        return entities;
    }

    #endregion

    #region Helpers

    public void UpdateBountyConsoles()
    {
        var query = EntityQueryEnumerator<XenobiologyBountyConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_station.GetOwningStation(uid) is not { } station
                || !TryComp<StationXenobiologyBountyDatabaseComponent>(station, out var db))
                continue;

            UpdateState(uid, db);
        }
    }

    private void UpdateState(EntityUid console, StationXenobiologyBountyDatabaseComponent db)
    {
        var untilNextSkip = db.NextSkipTime - _timing.CurTime;
        var untilNextRefresh = db.NextGlobalMarketRefresh - _timing.CurTime;
        var state = new XenobiologyBountyConsoleState(db.Bounties, db.History, untilNextSkip, untilNextRefresh);

        _xenoDatabase.SortBounties(db);
        _uiSystem.SetUiState(console, CargoConsoleUiKey.Bounty, state);
    }

    #endregion
}
