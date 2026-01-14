using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Components.Equipment;

/// <summary>
/// This handles the nozzle for xeno vacuums.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoVacuumComponent : Component
{
    /// <summary>
    /// The sound played when the vacuum is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/zzzt.ogg");

    /// <summary>
    /// The sound played when the tank is cleared.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ClearSound = new SoundPathSpecifier("/Audio/Effects/trashbag3.ogg");
}
