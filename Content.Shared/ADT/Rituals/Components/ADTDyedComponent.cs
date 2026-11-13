using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Rituals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ADTDyedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Dye;

    [DataField, AutoNetworkedField]
    public ProtoId<MarkingPrototype>? Marking;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTRitualTotemComponent : Component
{
}
