using Content.Shared.ADT.Heretic.Common;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed class FadingTimedDespawnSystem : SharedFadingTimedDespawnSystem
{
    protected override bool CanDelete(EntityUid uid)
    {
        return true;
    }
}
