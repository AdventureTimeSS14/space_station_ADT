using Content.Shared.ADT.Wizard.FadingTimedDespawn;

namespace Content.Server.ADT.Wizard.Systems;

public sealed class FadingTimedDespawnSystem : SharedFadingTimedDespawnSystem
{
    protected override bool CanDelete(EntityUid uid)
    {
        return true;
    }
}