using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingHeadslugComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    public bool IsInside = false;

    public float Accumulator = 0f;

    public float AccumulateTime = 90f;
}
