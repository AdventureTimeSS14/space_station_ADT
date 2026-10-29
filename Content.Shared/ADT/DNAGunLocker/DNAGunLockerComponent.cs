using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.DNAGunLocker;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DNAGunLockerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsEmagged;

    [DataField, AutoNetworkedField]
    public EntityUid? GunOwner;

    [DataField]
    public int ShockDamage = 10;

    [DataField]
    public TimeSpan ShockTime = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier SparkSound = new SoundCollectionSpecifier("sparks")
    {
        Params = AudioParams.Default.WithVolume(8),
    };

    [DataField]
    public SoundSpecifier LockSound = new SoundPathSpecifier("/Audio/ADT/dna-lock.ogg");

    [DataField]
    public SoundSpecifier ElectricSound = new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg");
}
