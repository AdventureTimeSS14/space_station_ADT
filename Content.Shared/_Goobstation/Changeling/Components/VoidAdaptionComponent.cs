using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.Changeling.Components;

/// <summary>
/// Marks a changeling that has evolved Void Adaption.
/// While this component is present, the server grants the entity immunity to pressure, temperature
/// and asphyxiation by attaching the base immunity components (see the server VoidAdaptionSystem).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VoidAdaptionComponent : Component
{
    /// <summary>
    /// Status alert shown while Void Adaption is active.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "VoidAdaption";
}
