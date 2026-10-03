using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;

namespace Content.Server.ADT.Ninja;

public sealed class NinjaADTConditionsSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly NumberObjectiveSystem _number = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainScanConditionComponent, ObjectiveGetProgressEvent>(OnBrainScanGetProgress);
        SubscribeLocalEvent<BorgHackConditionComponent, ObjectiveGetProgressEvent>(OnBorgHackGetProgress);
        SubscribeLocalEvent<BorgHackConditionComponent, RequirementCheckEvent>(OnBorgHackRequirementCheck);

        SubscribeLocalEvent<EmagSiliconLawComponent, SiliconEmaggedEvent>(OnBorgEmagged);
    }

    private void OnBrainScanGetProgress(EntityUid uid, BrainScanConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        if (target == 0)
        {
            args.Progress = 1f;
            return;
        }
        args.Progress = MathF.Min(comp.ScansCompleted / (float) target, 1f);
    }

    private void OnBorgHackGetProgress(EntityUid uid, BorgHackConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        var target = _number.GetTarget(uid);
        if (target == 0)
        {
            args.Progress = 1f;
            return;
        }
        args.Progress = MathF.Min(comp.BorgsHacked / (float) target, 1f);
    }

    private void OnBorgHackRequirementCheck(EntityUid uid, BorgHackConditionComponent comp, ref RequirementCheckEvent args)
    {
        if (args.Cancelled)
            return;

        var query = EntityQueryEnumerator<EmagSiliconLawComponent>();
        while (query.MoveNext(out var borg, out _))
        {
            if (TerminatingOrDeleted(borg))
                continue;

            if (_emag.CheckFlag(borg, EmagType.Interaction))
                continue;

            return;
        }

        args.Cancelled = true;
    }

    private void OnBorgEmagged(Entity<EmagSiliconLawComponent> ent, ref SiliconEmaggedEvent args)
    {
        var user = args.user;
        if (!_mind.TryGetObjectiveComp<BorgHackConditionComponent>(user, out var condition))
            return;

        var borg = ent.Owner;
        if (condition.HackedBorgs.Contains(borg))
            return;

        if (condition.BorgsHacked >= condition.Required)
            return;

        condition.HackedBorgs.Add(borg);
        condition.BorgsHacked++;
        Dirty(user, condition);
    }
}
