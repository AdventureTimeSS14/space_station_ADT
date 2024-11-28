using Content.Server.Objectives.Components;
using Content.Shared.Changeling.Components;
using Robust.Shared.Random;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Systems;

public sealed class StealDnaConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealDnaConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<StealDnaConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);

        SubscribeLocalEvent<StealDnaConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<StealDnaConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        var maxSize = condition.Comp.MaxDnaCount;

        var minSize = condition.Comp.MinDnaCount;

        condition.Comp.AbsorbDnaCount = _random.Next(minSize, maxSize);

    }

    public void OnAfterAssign(Entity<StealDnaConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString(condition.Comp.ObjectiveText, ("count", condition.Comp.AbsorbDnaCount));

        var description = Loc.GetString(condition.Comp.DescriptionText, ("count", condition.Comp.AbsorbDnaCount));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);

    }
    private void OnGetProgress(EntityUid uid, StealDnaConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.OwnedEntity.HasValue)
        {
            var ling = args.Mind.OwnedEntity.Value;
            args.Progress = GetProgress(ling, args.MindId, args.Mind, comp);
        }
        else
            args.Progress = 0f;
    }

    private float GetProgress(EntityUid uid, EntityUid mindId, MindComponent mind, StealDnaConditionComponent comp)
    {
        // Не генокрад - не выполнил цель (да ладно.)
        if (!TryComp<ChangelingComponent>(uid, out var ling))
            return 0f;

        // Умер - не выполнил цель.
        if (!mind.CurrentEntity.HasValue || _mind.IsCharacterDeadIc(mind))
            return 0f;

        // Подсчёт требуемых и имеющихся ДНК
        var count = ling.DNAStolen;
        var result = count / comp.AbsorbDnaCount;
        result = Math.Clamp(result, 0, 1);
        return result;
    }
}
