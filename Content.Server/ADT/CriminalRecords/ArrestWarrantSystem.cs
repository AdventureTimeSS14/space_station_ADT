using Content.Server.Access.Systems;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Server.Traits;
using Content.Shared.ADT.CriminalRecords;
using Content.Shared.CriminalRecords;
using Content.Shared.Dataset;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.CriminalRecords;

public sealed class ArrestWarrantSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly CriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly FaxSystem _fax = default!;

    private const string WarrantReasonsDataset = "ArrestWarrantReasons";
    private const string OperatorNamesDataset = "CentComOperatorNames";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn,
            after: [typeof(TraitSystem), typeof(StationRecordsSystem)]);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!HasComp<ArrestWarrantComponent>(ev.Mob))
            return;

        if (!_idCard.TryFindIdCard(ev.Mob, out var id)
            || !TryComp<StationRecordKeyStorageComponent>(id.Owner, out var keyStorage)
            || keyStorage.Key is not { } key)
            return;

        if (!_records.TryGetRecord<CriminalRecord>(key, out var record))
        {
            _records.AddRecordEntry(key, new CriminalRecord());
            if (!_records.TryGetRecord<CriminalRecord>(key, out record))
                return;
        }

        var reason = Loc.GetString(_random.Pick(_proto.Index<LocalizedDatasetPrototype>(WarrantReasonsDataset).Values));
        var initiator = Loc.GetString("arrest-warrant-initiator");

        _criminalRecords.OverwriteStatus(key, record, SecurityStatus.Wanted, reason, initiator);
        _criminalRecords.TryAddHistory(key, Loc.GetString("arrest-warrant-history", ("reason", reason)), initiator);

        SendWarrantFax(ev, reason);
    }

    private void SendWarrantFax(PlayerSpawnCompleteEvent ev, string reason)
    {
        var station = _station.GetOwningStation(ev.Mob);
        if (station == null)
            return;

        var printout = new FaxPrintout(
            Loc.GetString("arrest-warrant-fax-content",
                ("station", Name(station.Value)),
                ("operator", GetRandomOperator()),
                ("name", ev.Profile.Name),
                ("reason", reason)),
            Loc.GetString("arrest-warrant-fax-paper-name"),
            null, null,
            "paper_stamp-centcom",
            [new StampDisplayInfo
            {
                StampedName = Loc.GetString("stamp-component-stamped-name-centcom"),
                StampedColor = Color.FromHex("#006600")
            }]);

        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var faxUid, out var fax))
        {
            if (!IsSecurityFax(faxUid) || _station.GetOwningStation(faxUid) != station)
                continue;

            _fax.Receive(faxUid, printout, null, fax);
        }
    }

    private string GetRandomOperator()
    {
        var dataset = _proto.Index<LocalizedDatasetPrototype>(OperatorNamesDataset);
        return Loc.GetString(_random.Pick(dataset.Values));
    }

    private bool IsSecurityFax(EntityUid uid) // todo: хардкод пиздец, ну а чё сделать если факсов СБ нет только по имени искать.
    {
        if (!TryComp<FaxMachineComponent>(uid, out var fax))
            return false;

        var name = fax.FaxName.Trim().ToLower();

        if (name is "security" or "brig" or "hos" or "warden" or "detective" or "prison" or "perma"
            or "бриг" or "гсб" or "пермабриг" or "охрана")
            return true;

        if (name.StartsWith("head of security") || name.StartsWith("hos ")
            || name.StartsWith("hos's") || name.StartsWith("warden") || name.StartsWith("detective")
            || name.StartsWith("prison ") || name.StartsWith("perma"))
            return true;

        if (name.StartsWith("бриг") || name.StartsWith("офис гсб") || name.StartsWith("офис сб")
            || name.StartsWith("приемная сб") || name.StartsWith("брифинговая зона сб")
            || name.EndsWith("сб") || name.EndsWith("гсб") || name.Contains("пермабриг"))
            return true;

        return false;
    }
}