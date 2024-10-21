using Robust.Shared.Audio;

namespace Content.Server.DNALocker;

[RegisterComponent]
public sealed partial class DNALockerComponent : Component
{
    [DataField]
    public string DNA = string.Empty;

    public bool IsLocked => DNA != string.Empty;

    [DataField]
    public bool IsEquipped = false;

    [DataField]
    public bool CanBeEmagged = true;

    [DataField("lockSound")]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/ADT/dna-lock.ogg");

    [DataField("emagSound")]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    [DataField("lockerExplodeSound")]
    public SoundSpecifier LockerExplodeSound = new SoundPathSpecifier("/Audio/Effects/Grenades/SelfDestruct/SDS_Charge.ogg");

    [DataField("deniedSound")]
    public SoundSpecifier DeniedSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");
}
