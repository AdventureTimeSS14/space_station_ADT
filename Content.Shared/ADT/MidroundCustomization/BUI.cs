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
                            Dictionary<MarkingCategories, List<Marking>> markings, Dictionary<MarkingCategories, int> slotsTotal)
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
    }

    public string Species;
    public Sex Sex;

    public Dictionary<MarkingCategories, List<Marking>> Markings = new();
    public Dictionary<MarkingCategories, int> SlotsTotal = new();
    public bool AllowColorChanges;

    public string? TTS;
    public string BarkProto;
    public float BarkPitch;
    public float BarkMinVar;
    public float BarkMaxVar;
}

[Serializable, NetSerializable]
public abstract class GenericMidroundCustomizationMessage : BoundUserInterfaceMessage
{
    public GenericMidroundCustomizationMessage(MarkingCategories category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public MarkingCategories Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationMarkingSelectMessage : GenericMidroundCustomizationMessage
{
    public MidroundCustomizationMarkingSelectMessage(MarkingCategories category, string marking, int slot) : base(category, slot)
    {
        Marking = marking;
    }

    public string Marking { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationChangeColorMessage : GenericMidroundCustomizationMessage
{
    public MidroundCustomizationChangeColorMessage(MarkingCategories category, List<Color> colors, int slot) : base(category, slot)
    {
        Colors = colors;
    }

    public List<Color> Colors { get; }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationRemoveSlotMessage : GenericMidroundCustomizationMessage
{
    public MidroundCustomizationRemoveSlotMessage(MarkingCategories category, int slot) : base(category, slot)
    {
    }
}

[Serializable, NetSerializable]
public sealed class MidroundCustomizationAddSlotMessage : BoundUserInterfaceMessage
{
    public MidroundCustomizationAddSlotMessage(MarkingCategories category)
    {
        Category = category;
    }

    public MarkingCategories Category { get; }
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
