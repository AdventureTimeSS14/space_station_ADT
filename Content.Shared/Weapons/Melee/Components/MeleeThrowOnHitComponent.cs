using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weapons.Melee.Components;

/// <summary>
/// This is used for a melee weapon that throws whatever gets hit by it in a line
/// until it hits a wall or a time limit is exhausted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MeleeThrowOnHitSystem))]
public sealed partial class MeleeThrowOnHitComponent : Component
{
    /// <summary>
    /// The speed at which hit entities should be thrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Speed = 10f;

    /// <summary>
    /// The maximum distance the hit entity should be thrown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Distance = 20f;

    /// <summary>
    /// Whether or not anchorable entities should be unanchored when hit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool UnanchorOnHit;

    /// <summary>
    /// How long should this stun the target, if applicable?
    /// </summary>
<<<<<<< HEAD
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool Enabled = true;

    // ADT tweak start
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? CollideDamage;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? ToCollideDamage;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool DownOnHit = false;
    // ADT tweak end
=======
    [DataField, AutoNetworkedField]
    public TimeSpan? StunTime;

    /// <summary>
    /// Should this also work on a throw-hit?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ActivateOnThrown;
>>>>>>> e8c13fe325c5de84c2ec31ac5c70f254cf9333f3
}

/// <summary>
/// Raised a weapon entity with <see cref="MeleeThrowOnHitComponent"/> to see if a throw is allowed.
/// </summary>
<<<<<<< HEAD
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(MeleeThrowOnHitSystem))]
public sealed partial class MeleeThrownComponent : Component
{
    /// <summary>
    /// The velocity of the throw
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Vector2 Velocity;

    /// <summary>
    /// How long the throw will last.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Lifetime;

    /// <summary>
    /// How long we wait to start accepting collision.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinLifetime;

    /// <summary>
    /// At what point in time will the throw be complete?
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan ThrownEndTime;

    /// <summary>
    /// At what point in time will the <see cref="MinLifetime"/> be exhausted
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField]
    public TimeSpan MinLifetimeTime;

    /// <summary>
    /// the status to which the entity will return when the thrown ends
    /// </summary>
    [DataField]
    public BodyStatus PreviousStatus;

    // ADT tweak start
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? CollideDamage;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? ToCollideDamage;
    // ADT tweak end
}
=======
[ByRefEvent]
public record struct AttemptMeleeThrowOnHitEvent(EntityUid Target, EntityUid? User, bool Cancelled = false, bool Handled = false);
>>>>>>> e8c13fe325c5de84c2ec31ac5c70f254cf9333f3

/// <summary>
/// Raised a target entity before it is thrown by <see cref="MeleeThrowOnHitComponent"/>.
/// </summary>
[ByRefEvent]
public record struct MeleeThrowOnHitStartEvent(EntityUid Weapon, EntityUid? User);
