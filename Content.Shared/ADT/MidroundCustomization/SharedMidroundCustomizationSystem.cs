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

    public string Species = string.Empty;
    public Sex Sex;

    public string? Voice;

    public string? BarkProto;
    public float BarkPitch = 1f;
    public float BarkMinVar = 0.1f;
    public float BarkMaxVar = 0.5f;

    public bool PointLightColor;
    public bool PointLightColorEnabled;
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeVoiceMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationChangeVoiceMessage(string voice)
    {
        Voice = voice;
    }

    public string Voice { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeBarkMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationChangeBarkMessage(string proto, float pitch, float minVar, float maxVar)
    {
        Proto = proto;
        Pitch = pitch;
        MinVar = minVar;
        MaxVar = maxVar;
    }

    public string Proto { get; }
    public float Pitch { get; }
    public float MinVar { get; }
    public float MaxVar { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationPointLightColorToggleMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationPointLightColorToggleMessage(bool enabled)
    {
        Enabled = enabled;
    }

    public bool Enabled { get; }
}

[Serializable, NetSerializable]
public sealed partial class MidroundCustomizationSelectDoAfterEvent : DoAfterEvent
{
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings = new();

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class MidroundCustomizationChangeVoiceDoAfterEvent : DoAfterEvent
{
    public string Voice = string.Empty;

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class MidroundCustomizationChangeBarkDoAfterEvent : DoAfterEvent
{
    public string Proto = string.Empty;
    public float Pitch = 1f;
    public float MinVar = 0.1f;
    public float MaxVar = 0.5f;

    public override DoAfterEvent Clone() => this;
}

public sealed partial class MidroundCustomizationActionEvent : InstantActionEvent
{
}
