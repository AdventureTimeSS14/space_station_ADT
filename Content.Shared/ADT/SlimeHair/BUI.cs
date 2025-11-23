using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SlimeHair;

[Serializable, NetSerializable]
public enum SlimeHairUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SlimeHairUiState : BoundUserInterfaceState
{
    public SlimeHairUiState(string species, Sex sex, bool allowColors, string tts, string bark,
                            Dictionary<MarkingCategories, List<Marking>> markings, Dictionary<MarkingCategories, int> slotsTotal)
    {
        Species = species;
        Sex = sex;

        Markings = markings;
        SlotsTotal = slotsTotal;
        AllowColorChanges = allowColors;

        TTS = tts;
        Bark = bark;
    }

    public string Species;
    public Sex Sex;

    public Dictionary<MarkingCategories, List<Marking>> Markings = new();
    public Dictionary<MarkingCategories, int> SlotsTotal = new();
    public bool AllowColorChanges;

    public string TTS;
    public string Bark;
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
    public MidroundCustomizationRemoveSlotMessage(MarkingCategories category, List<Color> colors, int slot) : base(category, slot)
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
