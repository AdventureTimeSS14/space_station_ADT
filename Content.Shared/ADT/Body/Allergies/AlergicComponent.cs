using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Allergies;

[RegisterComponent, NetworkedComponent]
public sealed partial class AllergicComponent : Component
{
    [DataField]
    public List<ProtoId<ReagentPrototype>> Triggers = new();
}
