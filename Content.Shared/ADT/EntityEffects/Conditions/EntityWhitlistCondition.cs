using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityConditions.Conditions;
public sealed partial class WhitelistEntityConditionSystem : EntityConditionSystem<MetaDataComponent, WhitelistCondition>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    protected override void Condition(Entity<MetaDataComponent> entity, ref EntityConditionEvent<WhitelistCondition> args)
    {
        if (!args.Condition.Blacklist)
            args.Result = _whitelist.IsWhitelistFail(args.Condition.Whitelist, entity);
        else
            args.Result = _whitelist.IsBlacklistFail(args.Condition.Whitelist, entity);
    }
}

public sealed partial class WhitelistCondition : EntityConditionBase<WhitelistCondition>
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist;
    [DataField]
    public bool Blacklist = false;
    public override string EntityConditionGuidebookText(IPrototypeManager prototype) => String.Empty;
}
