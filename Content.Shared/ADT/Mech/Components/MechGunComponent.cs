using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mech.Equipment.Components;

/// <summary>
/// A gun. For a mech.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechGunComponent : Component
{
    [DataField]
    public float BatteryUsageMultiplier = 1f;
}

public enum MechShootType
{
    Projectile,
    Hitscan,
}

public sealed class MechShootEvent : CancellableEntityEventArgs
{
    public EntityUid User;

    public MechShootEvent(EntityUid user)
    {
        User = user;
    }
}

