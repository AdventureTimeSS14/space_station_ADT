using Content.Shared.Whitelist;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that removes minds with a blacklist objective.
/// </summary>
public sealed partial class ObjectiveMindFilter : MindFilter
{
    [DataField(required: true)]
    public EntityWhitelist Blacklist = new();

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var whitelistSys = entMan.System<EntityWhitelistSystem>();
        foreach (var obj in mind.Comp.Objectives)
        {
            // mind has a blacklisted objective, remove it from the pool
<<<<<<< HEAD
            if (whitelistSys.IsBlacklistPass(Blacklist, obj))
=======
            if (whitelistSys.IsWhitelistPass(Blacklist, obj))
>>>>>>> upstreamwiz/master
                return true;
        }

        return false;
    }
}
