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

        args.Progress = GetProgress(args.MindId, args.Mind, target.Value, comp.RequireDead);
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
        var allHumans = _mind.GetAliveHumansExcept(args.MindId);
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

        _target.SetTargetDna(uid, _random.Pick(allHumans), target);
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
        var allHumans = _mind.GetAliveHumansExcept(args.MindId);
        if (allHumans.Count == 0)
        {
            args.Cancelled = true;
            return;
        }

        var allHeads = new List<EntityUid>();
        foreach (var mind in allHumans)
        {
            // RequireAdminNotify used as a cheap way to check for command department
            if (_job.MindTryGetJob(mind, out _, out var prototype) && prototype.RequireAdminNotify)
                allHeads.Add(mind);
        }

        if (allHeads.Count == 0)
            allHeads = allHumans; // fallback to non-head target

        if (!TryComp<DnaComponent>(uid, out var reqiredDna))
        {
            args.Cancelled = true;
            return;
        }

        _target.SetTargetDna(uid, _random.Pick(allHeads), target);
    }

    private float GetProgress(EntityUid mindId, MindComponent mind, EntityUid target, bool requireDead)
    {
        // Цель удалена/гибнута
        if (!TryComp<MindComponent>(target, out var targetMind) || targetMind.OwnedEntity == null)
            return 0f;

        // Генокрада не существует?
        if (!TryComp<MindComponent>(mindId, out _))
            return 0f;

        // Если генокрад в форме обезьяны, например
        if (!TryComp<DnaComponent>(mindId, out var dna))
            return 0f;

        // Если у цели нет днк
        if (!TryComp<DnaComponent>(target, out var targetDna))
            return 0f;

        // ДНК соответствует, но нужно ещё улететь и убить цель
        if (targetDna.DNA == dna.DNA)
            return 0.33f;

        // Цель гибнута и ДНК соответствуют
        if (targetMind.OwnedEntity == null && targetDna.DNA == dna.DNA)
            return 0.66f;

        // Цель мертва
        if (_mind.IsCharacterDeadIc(targetMind) && targetDna.DNA == dna.DNA)
            return 0.66f;

        // Генокрад улетает под видом цели
        if (mind.OwnedEntity != null && TryComp<CuffableComponent>(mind.OwnedEntity, out var cuffed) && cuffed.CuffedHandCount > 0 && _emergencyShuttle.ShuttlesLeft && _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) && targetDna.DNA == dna.DNA)
            return 1f;

        // Если цель должна быть мертва, не проверяем эвак
        if (requireDead)
            return 0f;

        // Какой извращенец будет отключать эвак?
        if (!_config.GetCVar(CCVars.EmergencyShuttleEnabled))
            return 0f;

        // Цель улетает, личность не украдена
        if (targetMind.OwnedEntity != null && _emergencyShuttle.IsTargetEscaping(targetMind.OwnedEntity.Value))
            return 0f;

        // Эвак улетел без цели, класс.
        if (mind.OwnedEntity != null && _emergencyShuttle.ShuttlesLeft && _emergencyShuttle.IsTargetEscaping(mind.OwnedEntity.Value) && targetDna.DNA == dna.DNA)
            return 1f;

        // Всё ещё нужно остаться на свободе.
        if (TryComp<CuffableComponent>(mind.OwnedEntity, out var cuff) && cuff.CuffedHandCount > 0)
            return 0f;

        // Эвак ждёт, а цель ещё не пришла к нему. Ещё и жива, падаль.
        return _emergencyShuttle.EmergencyShuttleArrived ? 0.5f : 0f;
    }
}
