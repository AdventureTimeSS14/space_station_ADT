using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.BowsSystem.Components;
[RegisterComponent, NetworkedComponent]
public sealed partial class ExpendedBowsComponent : Component
{
    public TimeSpan coldownStart = TimeSpan.Zero;
    public TimeSpan coldown = TimeSpan.FromSeconds(3f);
    public bool IsHoldingKey;
    public int StepOfTension=0;
}