using System.Linq;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Random;

namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem
{
    public Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)> AssignJobsByPriority(
        Dictionary<NetUserId, HumanoidCharacterProfile> profiles,
        IReadOnlyList<EntityUid> stations)
    {
        DebugTools.Assert(stations.Count > 0);

        if (profiles.Count == 0)
            return new();

        profiles = profiles.ShallowClone();

        var assigned = new Dictionary<NetUserId, (ProtoId<JobPrototype>?, EntityUid)>(profiles.Count);
        var stationJobs = new Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>>();

        foreach (var station in stations)
        {
            stationJobs.Add(station, GetJobs(station).ToDictionary(x => x.Key, x => x.Value));
        }

        var jobCandidates = GetJobCandidates(profiles);
        var playerCandidates = GetPlayerCandidates(jobCandidates);

        for (var priority = JobPriority.High; priority > JobPriority.Never; priority--)
        {
            foreach (var station in stations)
            {
                var players = playerCandidates.Keys.ToList();
                _random.Shuffle(players);

                foreach (var player in players)
                {
                    if (TryPickJobWithPriority(player, priority, station, stationJobs, playerCandidates, out var job))
                        AssignPlayer(player, job, station, stationJobs, jobCandidates, playerCandidates, profiles, assigned);
                }
            }
        }

        return assigned;
    }

    private bool TryPickJobWithPriority(
        NetUserId player,
        JobPriority priority,
        EntityUid station,
        Dictionary<EntityUid, Dictionary<ProtoId<JobPrototype>, int?>> stationJobs,
        Dictionary<NetUserId, Dictionary<JobPriority, List<ProtoId<JobPrototype>>>> playerCandidates,
        out ProtoId<JobPrototype> job)
    {
        job = default;

        if (!playerCandidates.TryGetValue(player, out var candidates)
            || !candidates.TryGetValue(priority, out var jobs))
        {
            return false;
        }

        var availableJobs = jobs
            .Where(jobId => stationJobs[station].TryGetValue(jobId, out var slots) && slots is null or > 0)
            .ToList();

        if (availableJobs.Count == 0)
            return false;

        job = _random.Pick(availableJobs);
        return true;
    }
}
