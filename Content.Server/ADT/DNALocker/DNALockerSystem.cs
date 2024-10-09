using Robust.Shared.Audio;

namespace Content.Server.DNALocker;

[RegisterComponent]
public sealed partial class DNALockerComponent : Component
{
    [DataField]
    public string? DNA;

    [DataField]
    public bool Locked = false;

    [DataField("lockSound")]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/ADT/dna-lock.ogg");

    [DataField]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    [DataField]
    public SoundSpecifier LockerExplodeSound = new SoundPathSpecifier("/Audio/Effects/Grenades/SelfDestruct/SDS_Charge.ogg");
}