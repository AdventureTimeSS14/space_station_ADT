using Content.Server.ADT.CrewManifest;
using Content.Server.Station.Systems;
using Content.Shared.CrewManifest;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.CrewManifest;

public sealed class ADTSiliconCrewManifestSystem : EntitySystem
{
    private static readonly ProtoId<JobPrototype> BorgJobId = "Borg";
    private static readonly ProtoId<JobPrototype> StationAiJobId = "StationAi";
    private static readonly ProtoId<NpcFactionPrototype> NanoTrasenFactionId = "NanoTrasen";

    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private SharedRoleSystem _roleSystem = default!;
    [Dependency] private SharedJobSystem _jobSystem = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrewManifestEntriesCollectEvent>(OnCollectManifestEntries);
    }

    private void OnCollectManifestEntries(CrewManifestEntriesCollectEvent args)
    {
        var borgJob = _protoManager.Index(BorgJobId);
        var stationAiJob = _protoManager.Index(StationAiJobId);

        var borgQuery = EntityQueryEnumerator<BorgChassisComponent>();
        while (borgQuery.MoveNext(out var uid, out var chassis))
        {
            if (chassis.BrainEntity is not { } brain)
                continue;

            if (!_npcFactionSystem.IsMember(uid, NanoTrasenFactionId))
                continue;

            if (_stationSystem.GetOwningStation(uid) != args.Station)
                continue;

            if (!IsCrewSiliconMind(uid, BorgJobId))
                continue;

            args.Entries.Add((borgJob, BuildSiliconEntry(uid, borgJob)));
        }

        var aiQuery = EntityQueryEnumerator<StationAiCoreComponent>();
        while (aiQuery.MoveNext(out var uid, out var core))
        {
            if (!_stationAiSystem.TryGetHeld((uid, core), out var held))
                continue;

            if (_stationSystem.GetOwningStation(uid) != args.Station)
                continue;

            if (!IsCrewSiliconMind(held.Value, StationAiJobId))
                continue;

            args.Entries.Add((stationAiJob, BuildSiliconEntry(held.Value, stationAiJob)));
        }
    }

    private bool IsCrewSiliconMind(EntityUid uid, ProtoId<JobPrototype> expectedJob)
    {
        if (!_mindSystem.TryGetMind(uid, out var mindId, out _))
            return false;

        if (!_roleSystem.MindHasRole<SiliconBrainRoleComponent>(mindId))
            return false;

        if (_roleSystem.MindHasRole<SubvertedSiliconRoleComponent>(mindId))
            return false;

        if (_jobSystem.MindHasJobWithId(mindId, expectedJob))
            return true;

        return expectedJob == BorgJobId;
    }

    private CrewManifestEntry BuildSiliconEntry(EntityUid uid, JobPrototype job)
    {
        return new CrewManifestEntry(
            MetaData(uid).EntityName,
            job.LocalizedName,
            job.Icon.ToString(),
            job.ID);
    }
}
