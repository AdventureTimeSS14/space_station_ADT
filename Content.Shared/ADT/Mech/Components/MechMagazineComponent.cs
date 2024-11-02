using Robust.Shared.GameStates;

namespace Content.Server.ADT.Mech.Equipment.Components;

/// <summary>
/// A piece of mech equipment that grabs entities and stores them
/// inside of a container so large objects can be moved.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MechMagazineComponent : Component
{
    /// <summary>
    /// The change in energy after each drill.
    /// </summary>
    [DataField("magazinetype", required: true)]
    public string MagazineType;
}

