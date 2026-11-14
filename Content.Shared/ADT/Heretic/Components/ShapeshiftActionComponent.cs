using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShapeshiftActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<ProtoId<PolymorphPrototype>> Polymorphs = new();

    [DataField]
    public LocId Speech = "heretic-speech-shapeshft";
}
