using Content.Server.Objectives.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Content.Server.Forensics;
using Content.Shared.Cuffs.Components;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles kill person condition logic and picking random kill targets.
/// </summary>
public sealed class StealPersonalityConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergencyShuttle = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly TargetObjectiveSystem _target = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealPersonalityConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

        SubscribeLocalEvent<PickRandomDnaComponent, ObjectiveAssignedEvent>(OnPersonAssigned);

        SubscribeLocalEvent<PickRandomHeadDnaComponent, ObjectiveAssignedEvent>(OnHeadAssigned);
    }

    private void OnGetProgress(EntityUid uid, StealPersonalityConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (!_target.GetTarget(uid, out var target))
            return;

        args.Progress = GetProgress(args.MindId, args.Mind, target.Value, comp.ReqiredDNA ?? "", comp.RequireDead);
    }

    private void OnPersonAssigned(EntityUid uid, PickRandomDnaComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid objective prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumans(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        if (!TryComp<DnaComponent>(uid, out var reqiredDna))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<StealPersonalityConditionComponent>(uid, out var pers))
            pers.ReqiredDNA = reqiredDna.DNA;
    }

    private void OnHeadAssigned(EntityUid uid, PickRandomHeadDnaComponent comp, ref ObjectiveAssignedEvent args)
    {
        // invalid prototype
        if (!TryComp<TargetObjectiveComponent>(uid, out var target))
        {
            args.Cancelled = true;
            return;
        }

        // target already assigned
        if (target.Target != null)
            return;

        // no other humans to kill
        var allHumans = _mind.GetAliveHumans(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var allHeads = new HashSet<Entity<MindComponent>>();
        foreach (var mind in allHumans)
        {
            // RequireAdminNotify used as a cheap way to check for command department
            if (_job.MindTryGetJob(mind, out var prototype) && prototype.RequireAdminNotify)
                allHeads.Add(mind);
        }

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        if (!TryComp<DnaComponent>(uid, out var reqiredDna))
        {
            args.Cancelled = true;
            return;
        }
        if (TryComp<StealPersonalityConditionComponent>(uid, out var pers))
            pers.ReqiredDNA = reqiredDna.DNA;
    }

    private float GetProgress(EntityUid mindId, MindComponent mind, EntityUid target, string targetDna, bool requireDead)
    {
        // Генокрада не существует?
        if (!TryComp<MindComponent>(mindId, out _))
            return 0f;

        // Если генокрад в форме обезьяны, например
        if (!TryComp<DnaComponent>(mind.CurrentEntity, out var dna))
            return 0f;

        if (_emergencyShuttle.ShuttlesLeft && _emergencyShuttle.IsTargetEscaping(mind.CurrentEntity.Value))
        {
            if (_emergencyShuttle.IsTargetEscaping(target))
                return 0f;
            if (dna.DNA == targetDna && TryComp<CuffableComponent>(mind.CurrentEntity, out var cuffed) && cuffed.CuffedHandCount > 0)
                return 1f;
        }
        else
        {
            // ДНК соответствует, но нужно ещё улететь и убить цель
            if (targetDna == dna.DNA)
                return 0.5f;
            else
                return 0f;
        }

        // Эвак ждёт, а цель ещё не пришла к нему. Ещё и жива, падаль.
        return _emergencyShuttle.EmergencyShuttleArrived ? 0.5f : 0f;
    }
}
