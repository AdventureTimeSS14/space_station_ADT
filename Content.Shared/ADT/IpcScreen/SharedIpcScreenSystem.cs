using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;
using Content.Shared.Actions;
using Content.Shared.Humanoid;

namespace Content.Shared.ADT.IpcScreen;

[Serializable, NetSerializable]
public enum IpcScreenUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum IpcScreenCategory : byte
{
    FacialHair
}

[Serializable, NetSerializable]
public sealed class IpcScreenSelectMessage : BoundUserInterfaceMessage
{
    public IpcScreenSelectMessage(IpcScreenCategory category, string marking, int slot)
    {
        Category = category;
        Marking = marking;
        Slot = slot;
    }

    public IpcScreenCategory Category { get; }
    public string Marking { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class IpcScreenChangeColorMessage : BoundUserInterfaceMessage
{
    public IpcScreenChangeColorMessage(IpcScreenCategory category, List<Color> colors, int slot)
    {
        Category = category;
        Colors = colors;
        Slot = slot;
    }

    public IpcScreenCategory Category { get; }
    public List<Color> Colors { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class IpcScreenRemoveSlotMessage : BoundUserInterfaceMessage
{
    public IpcScreenRemoveSlotMessage(IpcScreenCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public IpcScreenCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class IpcScreenSelectSlotMessage : BoundUserInterfaceMessage
{
    public IpcScreenSelectSlotMessage(IpcScreenCategory category, int slot)
    {
        Category = category;
        Slot = slot;
    }

    public IpcScreenCategory Category { get; }
    public int Slot { get; }
}

[Serializable, NetSerializable]
public sealed class IpcScreenAddSlotMessage : BoundUserInterfaceMessage
{
    public IpcScreenAddSlotMessage(IpcScreenCategory category)
    {
        Category = category;
    }

    public IpcScreenCategory Category { get; }
}

[Serializable, NetSerializable]
public sealed class IpcScreenChangeVoiceMessage : BoundUserInterfaceMessage
{
    public IpcScreenChangeVoiceMessage(string voice)
    {
        Voice = voice;
    }

    public string Voice { get; }
}

[Serializable, NetSerializable]
public sealed class IpcScreenUiState : BoundUserInterfaceState
{
    public IpcScreenUiState(string species, Sex sex, string voice, List<Marking> facialHair, int facialHairSlotTotal)
    {
        Species = species;
        Sex = sex;
        Voice = voice;
        FacialHair = facialHair;
        FacialHairSlotTotal = facialHairSlotTotal;
    }

    public NetEntity Target;

    public string Species;
    public Sex Sex;
    public string Voice;

    public List<Marking> FacialHair;
    public int FacialHairSlotTotal;
}

[Serializable, NetSerializable]
public sealed partial class IpcScreenRemoveSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public IpcScreenCategory Category;
    public int Slot;
}

[Serializable, NetSerializable]
public sealed partial class IpcScreenAddSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public IpcScreenCategory Category;
}

[Serializable, NetSerializable]
public sealed partial class IpcScreenSelectDoAfterEvent : DoAfterEvent
{
    public IpcScreenCategory Category;
    public int Slot;
    public string Marking = string.Empty;

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class IpcScreenChangeColorDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public IpcScreenCategory Category;
    public int Slot;
    public List<Color> Colors = new List<Color>();
}

[Serializable, NetSerializable]
public sealed partial class IpcScreenChangeVoiceDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public string Voice = string.Empty;
}

public sealed partial class IpcScreenActionEvent : InstantActionEvent
{
}
