using Content.Shared.Anomaly.Effects;
using Content.Shared.Body.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Implants;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableStealthComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Toggled = false;

    [AutoNetworkedField]
    public EntityUid? ActionEntity;
}
