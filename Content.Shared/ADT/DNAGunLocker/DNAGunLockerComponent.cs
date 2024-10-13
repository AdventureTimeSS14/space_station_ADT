using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.DNAGunLocker;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class DNAGunLockerComponent : Component
{
    /// <summary>
    /// ADT. personableWeapon emagged
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsEmagged = false;

    /// <summary>
    /// Emag sound effects.
    /// </summary>
    [DataField("sparkSound")]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8),
    };

    [DataField("lockSound")]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/ADT/Effects/DNALocker/dna-lock.ogg");

    [DataField("electricSound")]
    public SoundSpecifier ElectricSound = new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg");

    //ADT TWEAK START
    /// <summary>
    /// ADT. personableWeapon field
    /// </summary>
    [DataField("gunOwner"), ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? GunOwner = null;
    //ADT END
}
