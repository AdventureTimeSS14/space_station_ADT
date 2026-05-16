using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.ADT.SlimeHair;

[Serializable, NetSerializable]
public enum SlimeHairUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum SlimeHairCategory : byte
{
    Hair,
    FacialHair
}

[Serializable, NetSerializable]
public sealed class SlimeHairSelectMessage : BoundUserInterfaceMessage
{
    public SlimeHairSelectMessage(SlimeHairCategory category, string marking, int slot)
    {
        Category = category;
        Marking = marking;
        Slot = slot;
    }

    public SlimeHairCategory Category { get; }
    public string Marking { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class SlimeHairChangeColorMessage : BoundUserInterfaceMessage
{
    public SlimeHairChangeColorMessage(SlimeHairCategory category, List<Color> colors, int slot)
    {
        Category = category;
        Colors = colors;
        Slot = slot;
    }

    public SlimeHairCategory Category { get; }
    public List<Color> Colors { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class SlimeHairRemoveSlotMessage : BoundUserInterfaceMessage
{
    public SlimeHairRemoveSlotMessage(SlimeHairCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public SlimeHairCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class SlimeHairSelectSlotMessage : BoundUserInterfaceMessage
{
    public SlimeHairSelectSlotMessage(SlimeHairCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public SlimeHairCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class SlimeHairAddSlotMessage : BoundUserInterfaceMessage
{
    public SlimeHairAddSlotMessage(SlimeHairCategory category)
    {
        Category = category;
    }

    public SlimeHairCategory Category { get; }
}

[Serializable, NetSerializable]
public sealed class SlimeHairUiState : BoundUserInterfaceState
{
    public SlimeHairUiState(string species, List<Marking> hair, int hairSlotTotal, List<Marking> facialHair, int facialHairSlotTotal)
    {
        Species = species;
        Hair = hair;
        HairSlotTotal = hairSlotTotal;
        FacialHair = facialHair;
        FacialHairSlotTotal = facialHairSlotTotal;
    }

    public NetEntity Target;

    public string Species;

    public List<Marking> Hair;
    public int HairSlotTotal;

    public List<Marking> FacialHair;
    public int FacialHairSlotTotal;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairRemoveSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public SlimeHairCategory Category;
    public int Slot;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairAddSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public SlimeHairCategory Category;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairSelectDoAfterEvent : DoAfterEvent
{
    public SlimeHairCategory Category;
    public int Slot;
    public string Marking = string.Empty;

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairChangeColorDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public SlimeHairCategory Category;
    public int Slot;
    public List<Color> Colors = new List<Color>();
}

public sealed partial class SlimeHairActionEvent : InstantActionEvent
{
}
