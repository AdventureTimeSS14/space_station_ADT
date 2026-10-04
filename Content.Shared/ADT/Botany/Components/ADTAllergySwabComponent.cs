using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Botany.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTAllergySwabComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>>? AllergicTriggers;
}
