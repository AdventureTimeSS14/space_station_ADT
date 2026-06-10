using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseNinjutsuEvent : EntityEventArgs
{
    [DataField]
    public virtual SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg");
}

public sealed partial class BiteTheDustPerformedEvent : BaseNinjutsuEvent;

public sealed partial class DirtyKillPerformedEvent : BaseNinjutsuEvent;
