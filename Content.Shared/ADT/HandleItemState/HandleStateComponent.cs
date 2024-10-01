using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.HandleItemState;

[RegisterComponent, NetworkedComponent]
public sealed partial class HandleItemStateComponent : Component
{
    /// <summary>
    ///     Включен ли HandleItemStateVisual изначально или нет
    /// </summary>
    [ViewVariables, DataField("enabled")]
    public bool Enabled;
}

[Serializable, NetSerializable]
public sealed class HandleItemState : ComponentState
{
    public bool Enabled;

    public HandleItemState(bool enabled)
    {
        Enabled = enabled;
    }
}
