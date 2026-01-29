using System.Threading;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Furniture.Tables.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RouletteComponent : Component
{
    [DataField, AutoNetworkedField]
    public RouletteState State = RouletteState.Idle;

    [DataField, AutoNetworkedField]
    public int Result = 0;

    [ViewVariables]
    public CancellationTokenSource? CancellationTokenSource;
}

[Serializable, NetSerializable]
public enum RouletteState : byte
{
    Idle,
    Rolling,
    Result
}

[Serializable, NetSerializable]
public enum RouletteVisuals : byte
{
    State,
    Result
}

[Serializable, NetSerializable]
public enum RouletteVisualLayers : byte
{
    Base
}
