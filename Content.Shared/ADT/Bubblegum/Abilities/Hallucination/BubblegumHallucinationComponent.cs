using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumHallucinationComponent : Component
{
    [DataField]
    public EntProtoId BloodOnDeathPrototype = "ADTPuddleBloodBubblegum";
}
