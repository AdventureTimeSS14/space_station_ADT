//

using Content.Shared.Heretic;

namespace Content.Shared.ADT.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    protected virtual void SubscribeLock()
    {
        SubscribeLocalEvent<HereticAscensionLockEvent>(OnAscensionLock);
    }

    private void OnAscensionLock(HereticAscensionLockEvent args)
    {
    }
}
