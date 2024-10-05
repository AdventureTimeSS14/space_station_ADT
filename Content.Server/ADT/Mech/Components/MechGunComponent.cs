namespace Content.Shared.ADT.Mech.Equipment.Components;

/// <summary>
/// A piece of mech equipment that grabs entities and stores them
/// inside of a container so large objects can be moved.
/// </summary>
[RegisterComponent]
public sealed partial class MechGunComponent : Component
{
}
public sealed class MechShootEvent : CancellableEntityEventArgs
{
    public EntityUid User;

    public MechShootEvent(EntityUid user)
    {
        User = user;
    }
}

