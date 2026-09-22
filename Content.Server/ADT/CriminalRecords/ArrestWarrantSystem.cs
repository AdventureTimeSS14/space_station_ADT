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

        EntityUid? hosFax = null;
        EntityUid? wardenFax = null;
        EntityUid? secFax = null;
        EntityUid? supportFax = null;
        EntityUid? captainFax = null;
        var query = EntityQueryEnumerator<FaxMachineComponent>();
        while (query.MoveNext(out var faxUid, out _))
        {
            if (_station.GetOwningStation(faxUid) != station)
                continue;

            if (hosFax == null && IsHosFax(faxUid))
                hosFax = faxUid;
            else if (wardenFax == null && IsWardenFax(faxUid))
                wardenFax = faxUid;
            else if (secFax == null && IsSecFax(faxUid))
                secFax = faxUid;
            else if (supportFax == null && IsSupportFax(faxUid))
                supportFax = faxUid;
            else if (captainFax == null && IsCaptainFax(faxUid))
                captainFax = faxUid;
        }

        var target = hosFax ?? wardenFax ?? secFax ?? supportFax ?? captainFax;
        if (target == null)
            return;

        _fax.Receive(target.Value, printout);
    }

    private string GetRandomOperator()
    {
        var dataset = _proto.Index<LocalizedDatasetPrototype>(OperatorNamesDataset);
        return Loc.GetString(_random.Pick(dataset.Values));
    }

    private string? GetFaxName(EntityUid uid)
    {
        return TryComp<FaxMachineComponent>(uid, out var fax) ? fax.FaxName.Trim().ToLower() : null;
    }

    private bool IsHosFax(EntityUid uid)
    {
        var name = GetFaxName(uid);
        if (name == null)
            return false;

        return name is "гсб" or "hos" or "head of security"
            || name.StartsWith("гсб") || name.StartsWith("офис гсб") || name.StartsWith("кабинет гсб")
            || name.StartsWith("кабинет главы службы безопасности")
            || name.StartsWith("head of security") || name.StartsWith("hos ")
            || name.StartsWith("hos's");
    }

    private bool IsWardenFax(EntityUid uid)
    {
        var name = GetFaxName(uid);
        if (name == null)
            return false;

        return name is "warden" or "warden's office" or "смотритель"
            || name.StartsWith("warden") || name.StartsWith("офис смотрителя")
            || name.StartsWith("кабинет смотрителя");
    }

    private bool IsSecFax(EntityUid uid)
    {
        var name = GetFaxName(uid);
        if (name == null)
            return false;

        return name is "security" or "security office" or "brig" or "prison" or "courtroom"
            or "бриг" or "сб" or "охрана"
            || name.StartsWith("security ") || name.StartsWith("brig ") || name.StartsWith("prison ")
            || name.StartsWith("бриг") || name.StartsWith("офис сб") || name.StartsWith("приемная сб")
            || name.StartsWith("брифинговая зона сб")
            || name.EndsWith("сб");
    }

    private bool IsSupportFax(EntityUid uid)
    {
        var name = GetFaxName(uid);
        if (name == null)
            return false;

        return name is "detective" or "detective's office" or "детектив"
            || name.StartsWith("detective") || name.StartsWith("офис детектива")
            || name.StartsWith("кабинет детектива") || name.StartsWith("офис бригмеда")
            || name.StartsWith("кабинет бригмеда");
    }

    private bool IsCaptainFax(EntityUid uid)
    {
        return TryComp<MetaDataComponent>(uid, out var meta)
            && meta.EntityPrototype?.ID == "FaxMachineCaptain";
    }
}