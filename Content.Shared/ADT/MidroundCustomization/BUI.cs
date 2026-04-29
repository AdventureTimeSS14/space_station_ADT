using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MidroundCustomization;

[Serializable, NetSerializable]
public enum MidroundCustomizationUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationUiState : BoundUserInterfaceState
{
    public MidroundCustomizationUiState(string species, Sex sex, bool allowColors, string tts,
                            string barkProto, float barkPitch, float barkMinVar, float barkMaxVar,
                            Dictionary<HumanoidVisualLayers, List<Marking>> markings, Dictionary<HumanoidVisualLayers, int> slotsTotal,
                            bool pointLightColor, bool pointLightColorEnabled)
    {
        Species = species;
        Sex = sex;

        Markings = markings;
        SlotsTotal = slotsTotal;
        AllowColorChanges = allowColors;

        TTS = tts;
        BarkProto = barkProto;
        BarkPitch = barkPitch;
        BarkMinVar = barkMinVar;
        BarkMaxVar = barkMaxVar;

        PointLightColor = pointLightColor;
        PointLightColorEnabled = pointLightColorEnabled;
    }

    public string Species;
    public Sex Sex;

    public Dictionary<HumanoidVisualLayers, List<Marking>> Markings = new();
    public Dictionary<HumanoidVisualLayers, int> SlotsTotal = new();
    public bool AllowColorChanges;

    public string? TTS;
    public string BarkProto;
    public float BarkPitch;
    public float BarkMinVar;
    public float BarkMaxVar;
    public bool PointLightColor;
    public bool PointLightColorEnabled;
}

[Serializable, NetSerializable]
public abstract class GenericMidroundCustomizationMessage : BoundUserInterfaceMessage
{
    public GenericMidroundCustomizationMessage(HumanoidVisualLayers layer, int slot)
    {
        Layer = layer;
        Slot = slot;
    }

    public HumanoidVisualLayers Layer { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationMarkingSelectMessage : GenericMidroundCustomizationMessage
{
    public MidroundCustomizationMarkingSelectMessage(HumanoidVisualLayers layer, string marking, int slot) : base(layer, slot)
    {
        Marking = marking;
    }

    public string Marking { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeColorMessage : GenericMidroundCustomizationMessage
{
    public MidroundCustomizationChangeColorMessage(HumanoidVisualLayers layer, List<Color> colors, int slot) : base(layer, slot)
    {
        Colors = colors;
    }

    public List<Color> Colors { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationRemoveSlotMessage : GenericMidroundCustomizationMessage
{
    public MidroundCustomizationRemoveSlotMessage(HumanoidVisualLayers layer, int slot) : base(layer, slot)
    {
    }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationAddSlotMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationAddSlotMessage(HumanoidVisualLayers layer)
    {
        Layer = layer;
    }

    public HumanoidVisualLayers Layer { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeVoiceMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationChangeVoiceMessage(string tts, string bark)
    {
        TTS = tts;
        Bark = bark;
    }

    public string TTS { get; }
    public string Bark { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeBarkProtoMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationChangeBarkProtoMessage(string proto)
    {
        Proto = proto;
    }

    public string Proto { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeBarkPitchMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationChangeBarkPitchMessage(float pitch)
    {
        Pitch = pitch;
    }

    public float Pitch { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeBarkMinVarMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationChangeBarkMinVarMessage(float minVar)
    {
        MinVar = minVar;
    }

    public float MinVar { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeBarkMaxVarMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationChangeBarkMaxVarMessage(float maxVar)
    {
        MaxVar = maxVar;
    }

    public float MaxVar { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationPointLightColorToggleMessage : BoundUserInterfaceMessage
{
    public bool Enabled { get; }

    public MidroundCustomizationPointLightColorToggleMessage(bool enabled)
    {
        Enabled = enabled;
    }
}
