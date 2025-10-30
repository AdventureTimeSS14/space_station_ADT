
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._SD.Weapons.SmartGun;

/// <summary>
/// Activates a laser pointer when wielding an item
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LaserPointerComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/SD/Weapons/Guns/Misc/Smartpistol_StartCast_1.ogg");

    [DataField]
    public SoundSpecifier? LockOnSound = new SoundPathSpecifier("/Audio/SD/Weapons/Guns/Misc/Smartpistol_Aim_1.ogg");

    [DataField(customTypeSerializer: typeof(FlagSerializer<CollisionMask>))]
    public int CollisionMask = (int) CollisionGroup.BulletImpassable;

    [DataField]
    public Color TargetedColor = Color.LawnGreen;

    [DataField]
    public Color DefaultColor = Color.Red;

    [ViewVariables]
    public TimeSpan LastNetworkEventTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan MaxDelayBetweenNetworkEvents = TimeSpan.FromSeconds(0.5);

    public EntityUid? CurrentLockedTarget;
    public bool WasLockedOnLastFrame = false;

}
