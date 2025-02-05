using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class ModifyGrabStageTimeComponent : Component
{
    [DataField(required: true)]
    public Dictionary<GrabStage, float> Modifiers = new();
}
