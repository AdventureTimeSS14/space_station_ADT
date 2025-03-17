using Content.Server.Objectives.Components;
using Content.Shared.Changeling.Components;
using Robust.Shared.Random;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.FixedPoint;

namespace Content.Server.Objectives.Systems;

public sealed class AbsorbChangelingConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AbsorbChangelingConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<AbsorbChangelingConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    public void OnAfterAssign(Entity<AbsorbChangelingConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString(condition.Comp.ObjectiveText);
        var description = Loc.GetString(condition.Comp.DescriptionText);

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);

    }
    private void OnGetProgress(EntityUid uid, AbsorbChangelingConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        if (args.Mind.CurrentEntity.HasValue)
        {
            var ling = args.Mind.CurrentEntity.Value;
            args.Progress = GetProgress(ling, args.Mind, comp);
        }
        else
            args.Progress = 0f;
    }

    private float GetProgress(EntityUid uid, MindComponent mind, AbsorbChangelingConditionComponent comp)
    {
        // Не генокрад - не выполнил цель (да ладно.)
        if (!TryComp<ChangelingComponent>(uid, out var ling))
            return 0f;

        // Умер - не выполнил цель.
        if (!_mind.IsCharacterDeadIc(mind))
            return 0f;

        // Подсчёт требуемых и имеющихся ДНК
        var count = ling.ChangelingsAbsorbed;
        var result = (float)count / (float)comp.AbsorbCount;
        result = Math.Clamp(result, 0, 1);
        return result;
    }
}
