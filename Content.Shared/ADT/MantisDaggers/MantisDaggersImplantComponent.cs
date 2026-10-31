using Content.Shared.ADT.Implants;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MantisDaggers;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class MantisDaggersImplantComponent : Component
{
    [DataField(required: true)]
    public MantisDaggerSide Side;
}



