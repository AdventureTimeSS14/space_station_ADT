using Content.Shared.DoAfter;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.ADT.IpcScreen;

[Serializable, NetSerializable]
public enum IpcScreenUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class IpcScreenSelectMessage : BoundUserInterfaceMessage
{
    public IpcScreenSelectMessage(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        Markings = markings;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; }
}

[Serializable, NetSerializable]
public sealed class IpcScreenUiState : BoundUserInterfaceState
{
    public IpcScreenUiState(Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles,
        Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> markings,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> applied)
    {
        OrganProfileData = profiles;
        OrganMarkingData = markings;
        AppliedMarkings = applied;
    }

    public NetEntity Target;

    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> OrganProfileData;
    public Dictionary<ProtoId<OrganCategoryPrototype>, OrganMarkingData> OrganMarkingData;
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> AppliedMarkings;
}

[Serializable, NetSerializable]
public sealed partial class IpcScreenSelectDoAfterEvent : DoAfterEvent
{
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();

    public override DoAfterEvent Clone() => this;
}

public sealed partial class IpcScreenActionEvent : InstantActionEvent
{
}
