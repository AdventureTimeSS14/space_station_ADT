using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MidroundCustomization;

[Serializable, NetSerializable]
public sealed partial class SlimeHairRemoveSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public MarkingCategories Category;
    public int Slot;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairAddSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public MarkingCategories Category;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairSelectDoAfterEvent : DoAfterEvent
{
    public MarkingCategories Category;
    public int Slot;
    public string Marking = string.Empty;

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairChangeColorDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public MarkingCategories Category;
    public int Slot;
    public List<Color> Colors = new List<Color>();
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairChangeVoiceDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public string Voice = string.Empty;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairChangeBarkProtoDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public string Proto = string.Empty;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairChangeBarkPitchDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public float Pitch;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairChangeBarkMinVarDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public float MinVar;
}

[Serializable, NetSerializable]
public sealed partial class SlimeHairChangeBarkMaxVarDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
    public float MaxVar;
}
