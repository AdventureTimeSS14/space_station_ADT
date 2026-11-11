using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Virology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTDiseaseVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public ADTDiseaseSeverity Severity;
}
