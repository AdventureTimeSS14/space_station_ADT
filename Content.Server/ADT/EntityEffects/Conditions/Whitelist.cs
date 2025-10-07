using Content.Server.Temperature.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.EffectConditions;

public sealed partial class Whitelist : EntityEffectCondition
{
    [DataField(required: true)]
    public EntityWhitelist List;

    [DataField]
    public bool Blacklist = false;

    public override bool Condition(EntityEffectBaseArgs args)
    {
        var whitelistSys = args.EntityManager.System<EntityWhitelistSystem>();
        if (Blacklist)
            return !whitelistSys.IsWhitelistPass(List, args.TargetEntity);
        else
            return whitelistSys.IsWhitelistPass(List, args.TargetEntity);
    }

    public override string GuidebookExplanation(IPrototypeManager prototype)
    {
        return String.Empty;
    }
}
