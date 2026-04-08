<<<<<<< HEAD
=======
using System.Numerics;
>>>>>>> upstreamwiz/master
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Randomly teleports the entity when triggered.
/// If TargetUser is true the user will be teleported instead.
/// Used for scram implants.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScramOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
<<<<<<< HEAD
    /// Up to how far to teleport the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TeleportRadius = 100f;
=======
    /// Up to how far to teleport the entity. Represented with X as Min Radius, and Y as Max Radius
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 TeleportRadius = new (10f, 15f);
>>>>>>> upstreamwiz/master

    /// <summary>
    /// the sound to play when teleporting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
}
