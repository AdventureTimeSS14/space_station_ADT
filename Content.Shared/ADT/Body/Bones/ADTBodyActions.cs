using Content.Shared.Alert;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Body.Bones;

[Serializable, NetSerializable]
public enum ADTBodyAction : byte
{
    ApplySplint,
    RemoveSplint,
    Reattach,
    RemoveShrapnelHands,
    RemoveShrapnelTool,
}

[Serializable, NetSerializable]
public enum ADTBodyActionUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTBodyActionOption(NetEntity organ, ADTBodyAction action, string limb, string label)
{
    public readonly NetEntity Organ = organ;
    public readonly ADTBodyAction Action = action;
    public readonly string Limb = limb;
    public readonly string Label = label;
}

[Serializable, NetSerializable]
public sealed class ADTBodyActionsBuiState(List<ADTBodyActionOption> options) : BoundUserInterfaceState
{
    public readonly List<ADTBodyActionOption> Options = options;
}

[Serializable, NetSerializable]
public sealed class ADTBodyActionSelectMessage(NetEntity organ, ADTBodyAction action) : BoundUserInterfaceMessage
{
    public readonly NetEntity Organ = organ;
    public readonly ADTBodyAction Action = action;
}

public sealed partial class ADTEmbeddedShrapnelAlertEvent : BaseAlertEvent;
