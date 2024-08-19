using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[RegisterComponent]
public sealed partial class EftposComponent : Component
{
    [ViewVariables]
    public int? BankAccountId;

    [ViewVariables]
    public int Amount;

    [DataField("soundApply")]
    public SoundSpecifier SoundApply = new SoundPathSpecifier("/Audio/ADT/Machines/chime.ogg");

    [DataField("soundDeny")]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/ADT/Machines/buzz-sigh.ogg");
}

[Serializable, NetSerializable]
public enum EftposKey
{
    Key
}
