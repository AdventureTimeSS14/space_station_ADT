using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingHeadslugComponent : Component
{
    public EntityUid? Container;

    public bool IsInside = false;

    public float Accumulator = 0f;

    public float AccumulateTime = 90f;

    public bool Alerted = false;
}
