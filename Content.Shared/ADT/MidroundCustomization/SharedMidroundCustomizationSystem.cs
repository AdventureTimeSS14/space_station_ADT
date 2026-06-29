using Content.Shared.DoAfter;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.ADT.MidroundCustomization;

[Serializable, NetSerializable]
public enum MidroundCustomizationUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationSelectMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationSelectMessage(Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        Markings = markings;
    }

    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationUiState : BoundUserInterfaceState
{
    public MidroundCustomizationUiState(Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData> profiles,
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
public sealed partial class MidroundCustomizationSelectDoAfterEvent : DoAfterEvent
{
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();

    public override DoAfterEvent Clone() => this;
}

public sealed partial class MidroundCustomizationActionEvent : InstantActionEvent
{
}