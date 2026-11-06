using Content.Shared.ADT.Mind;
using Content.Shared.ADT.Polls;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.ADT.Polls;

public sealed partial class ADTPollSystem
{
    private readonly Dictionary<NetUserId, PollSnapshot> _round = new();

    private void InitializeParticipation()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
    }

    private PollSnapshot GetRoundEntry(NetUserId userId)
    {
        if (!_round.TryGetValue(userId, out var entry))
        {
            entry = new PollSnapshot();
            _round[userId] = entry;
        }

        entry.InRound = true;
        return entry;
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var entry = GetRoundEntry(ev.Player.UserId);

        entry.Species.Add(ev.Profile.Species.Id);

        if (ev.JobId != null)
            AddJob(entry, ev.JobId);

        FillPlaytime(ev.Player, entry);

        TargetJoined(ev.Player);
    }

    private void OnRoleAdded(RoleAddedEvent ev)
    {
        if (IsDisposableMind(ev.MindId, ev.Mind))
            return;

        var userId = ev.Mind.OriginalOwnerUserId ?? ev.Mind.UserId;

        if (userId == null)
            return;

        RecordMind(userId.Value, ev.MindId, ev.Mind);

        if (_players.TryGetSessionById(userId.Value, out var session))
            TargetJoined(session);
    }

    private void RecordMind(NetUserId userId, EntityUid mindId, MindComponent mind)
    {
        var roles = _roles.MindGetAllRoleInfo((mindId, mind));
        roles.RemoveAll(r => string.IsNullOrEmpty(r.Prototype));

        HumanoidProfileComponent? humanoid = null;

        if (mind.OwnedEntity is { } body)
            TryComp(body, out humanoid);

        if (roles.Count == 0 && humanoid == null)
            return;

        var entry = GetRoundEntry(userId);

        foreach (var role in roles)
        {
            if (role.Antagonist)
            {
                entry.Antag = true;
                entry.AntagRoles.Add(role.Prototype);
                continue;
            }

            AddJob(entry, role.Prototype);
        }

        if (humanoid != null)
            entry.Species.Add(humanoid.Species.Id);

        if (_players.TryGetSessionById(userId, out var session))
            FillPlaytime(session, entry);
    }

    private void AddJob(PollSnapshot entry, string jobId)
    {
        if (!_prototype.HasIndex<JobPrototype>(jobId))
            return;

        entry.Jobs.Add(jobId);

        if (!_jobs.TryGetAllDepartments(jobId, out var departments))
            return;

        foreach (var department in departments)
        {
            entry.Departments.Add(department.ID);
        }
    }

    private void FillPlaytime(ICommonSession session, PollSnapshot snapshot)
    {
        if (!_playTime.TryGetTrackerTimes(session, out var times))
            return;

        snapshot.TrackerMinutes.Clear();

        foreach (var (tracker, time) in times)
        {
            snapshot.TrackerMinutes[tracker] = (int) time.TotalMinutes;
        }

        snapshot.PlaytimeMinutes = snapshot.TrackerMinutes.GetValueOrDefault(PlayTimeTrackingShared.TrackerOverall);
    }

    private bool IsDisposableMind(EntityUid mindId, MindComponent mind)
    {
        return mind.OwnedEntity is { } body
            && TryComp<TemporaryMindComponent>(body, out var temp)
            && temp.DisposableMind == mindId;
    }

    private void CollectRoundMinds()
    {
        var disposable = new HashSet<EntityUid>();
        var tempQuery = EntityQueryEnumerator<TemporaryMindComponent>();

        while (tempQuery.MoveNext(out _, out var temp))
        {
            disposable.Add(temp.DisposableMind);
        }

        var query = EntityQueryEnumerator<MindComponent>();

        while (query.MoveNext(out var mindId, out var mind))
        {
            if (disposable.Contains(mindId))
                continue;

            var userId = mind.OriginalOwnerUserId ?? mind.UserId;

            if (userId == null)
                continue;

            RecordMind(userId.Value, mindId, mind);
        }
    }

    private PollSnapshot BuildSnapshot(ICommonSession session)
    {
        var snapshot = _round.TryGetValue(session.UserId, out var entry)
            ? entry.Clone()
            : new PollSnapshot();

        FillPlaytime(session, snapshot);
        return snapshot;
    }
}
