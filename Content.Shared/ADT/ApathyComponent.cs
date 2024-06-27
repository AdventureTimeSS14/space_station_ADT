using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;

namespace Content.Shared.ADT;

/// <summary>
///     Just an RP feature. Could be used for more in future. For example, something like a virus.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ApathyComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> Alert = "ADTAlertApathy";
}
