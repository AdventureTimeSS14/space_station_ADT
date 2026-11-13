using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Shared.ADT.Ninja.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Ninja;

public sealed class NinjaADTConditionsSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private NumberObjectiveSystem _number = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BrainScanConditionComponent, ObjectiveGetProgressEvent>(OnBrainScanGetProgress);
        SubscribeLocalEvent<BorgHackConditionComponent, ObjectiveAssignedEvent>(OnBorgHackAssigned);
        SubscribeLocalEvent<BorgHackConditionComponent, ObjectiveAfterAssignEvent>(OnBorgHackAfterAssign);
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

    private void OnBorgHackAssigned(EntityUid uid, BorgHackConditionComponent comp, ref ObjectiveAssignedEvent args)
    {
        var activeBorgs = CountActiveBorgs();

        var min = comp.MinFactor > 0f ? (int) MathF.Ceiling(activeBorgs * comp.MinFactor) : comp.Min;
        var max = comp.MaxFactor > 0f ? (int) MathF.Ceiling(activeBorgs * comp.MaxFactor) : comp.Max;
        min = Math.Max(1, min);
        max = Math.Max(min, max);

        comp.Target = _random.Next(min, max + 1);

        if (activeBorgs < comp.Target)
        {
            args.Cancelled = true;
            return;
        }

        comp.Required = comp.Target;
    }

    private void OnBorgHackAfterAssign(EntityUid uid, BorgHackConditionComponent comp, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(uid, Loc.GetString("objective-condition-borg-hack-title", ("count", comp.Target)), args.Meta);
        _metaData.SetEntityDescription(uid, Loc.GetString("objective-condition-borg-hack-description", ("count", comp.Target)), args.Meta);
    }

    private void OnBorgHackGetProgress(EntityUid uid, BorgHackConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (comp.Target == 0)
        {
            args.Progress = 1f;
            return;
        }
        args.Progress = MathF.Min(comp.BorgsHacked / (float) comp.Target, 1f);
    }

    private int CountActiveBorgs()
    {
        var count = 0;
        var query = EntityQueryEnumerator<BorgChassisComponent>();
        while (query.MoveNext(out var borg, out var chassis))
        {
            if (!chassis.Active || HasComp<NinjaBorgChassisComponent>(borg))
                continue;

            count++;
        }

        return count;
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

    public bool TryRegisterHackedBorg(BorgHackConditionComponent condition, EntityUid borg)
    {
        if (condition.HackedBorgs.Contains(borg))
            return false;

        if (condition.BorgsHacked >= condition.Required)
            return false;

        condition.HackedBorgs.Add(borg);
        condition.BorgsHacked++;
        Dirty(condition.Owner, condition);
        return true;
    }

    private void OnBorgEmagged(Entity<EmagSiliconLawComponent> ent, ref SiliconEmaggedEvent args)
    {
        var user = args.user;
        if (!_mind.TryGetObjectiveComp<BorgHackConditionComponent>(user, out var condition))
            return;

        TryRegisterHackedBorg(condition, ent.Owner);
    }
}
