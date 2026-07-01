using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Silicons.Borgs.Components;

[RegisterComponent]
public sealed partial class AiRemoteControllerComponent : Component
{
    [DataField]
    public EntProtoId BackToAiAction = "ADTActionBackToAi";

    [ViewVariables]
    public EntityUid? BackToAiActionEntity;

    [ViewVariables]
    public EntityUid? AiHolder;

    [ViewVariables]
    public EntityUid? LinkedMind;

    [ViewVariables]
    public string[]? PreviouslyTransmitterChannels;

    [ViewVariables]
    public string[]? PreviouslyActiveRadioChannels;
}

[Serializable, NetSerializable]
public sealed class RemoteDeviceActionMessage : BoundUserInterfaceMessage
{
    public readonly RemoteDeviceActionEvent? RemoteAction;

    public RemoteDeviceActionMessage(RemoteDeviceActionEvent remoteDeviceAction)
    {
        RemoteAction = remoteDeviceAction;
    }
}

[Serializable, NetSerializable]
public sealed class RemoteDeviceActionEvent : EntityEventArgs
{
    public RemoteDeviceActionType ActionType;
    public NetEntity Target;

    public RemoteDeviceActionEvent(RemoteDeviceActionType actionType, NetEntity target)
    {
        ActionType = actionType;
        Target = target;
    }
}

[Serializable, NetSerializable]
public record struct RemoteDevicesData()
{
    public string DisplayName = string.Empty;

    public NetEntity NetEntityUid = NetEntity.Invalid;

    public SpriteSpecifier? Sprite;

    public bool IsIncapacitated;
}

[Serializable, NetSerializable]
public sealed class RemoteDevicesBuiState : BoundUserInterfaceState
{
    public List<RemoteDevicesData> DeviceList;

    public RemoteDevicesBuiState(List<RemoteDevicesData> deviceList)
    {
        DeviceList = deviceList;
    }
}

public enum RemoteDeviceActionType
{
    MoveToDevice,
    TakeControl
}