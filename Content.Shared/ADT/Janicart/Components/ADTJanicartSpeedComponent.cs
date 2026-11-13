using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Janicart.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedADTJanicartSystem))]
public sealed partial class ADTJanicartSpeedComponent : Component
{
    [DataField]
    public float SpeedMultiplier = 1.3f;
}