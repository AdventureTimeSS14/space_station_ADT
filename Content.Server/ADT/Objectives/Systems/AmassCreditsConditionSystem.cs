using Content.Server.ADT.Economy;
using Content.Server.ADT.Objectives.Components;
using Content.Shared.Cargo.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Stacks;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Objectives.Systems;

public sealed class AmassCreditsConditionSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private BankCardSystem _bankCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AmassCreditsConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<AmassCreditsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAfterAssign(Entity<AmassCreditsConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var title = Loc.GetString(condition.Comp.ObjectiveText, ("credits", condition.Comp.TargetCredits));
        var description = Loc.GetString(condition.Comp.DescriptionText, ("credits", condition.Comp.TargetCredits));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
    }

    private void OnGetProgress(Entity<AmassCreditsConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        if (condition.Comp.TargetCredits <= 0)
            return;

        var current = GetCredits((args.MindId, args.Mind));
        args.Progress = Math.Clamp(current / (float) condition.Comp.TargetCredits, 0f, 1f);
    }

    public int GetCredits(Entity<MindComponent> mind)
    {
        if (mind.Comp.OwnedEntity is not { } body)
            return 0;

        var total = _bankCard.TryGetUserAccountId(body, out var accountId)
            ? _bankCard.GetBalance(accountId)
            : 0;

        return total + CountCash(body);
    }

    private const int MaxContainerDepth = 10;

    private int CountCash(EntityUid body)
    {
        var total = 0;

        var toVisit = new Stack<(EntityUid Entity, int Depth)>();
        toVisit.Push((body, 0));

        while (toVisit.TryPop(out var current))
        {
            if (current.Depth > MaxContainerDepth)
                continue;

            if (!TryComp<ContainerManagerComponent>(current.Entity, out var manager))
                continue;

            foreach (var container in manager.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    if (HasComp<CashComponent>(contained) &&
                        TryComp<StackComponent>(contained, out var cashStack))
                        total += cashStack.Count;

                    if (HasComp<ContainerManagerComponent>(contained))
                        toVisit.Push((contained, current.Depth + 1));
                }
            }
        }

        return total;
    }
}
