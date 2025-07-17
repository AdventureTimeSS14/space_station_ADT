using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Server.SD.Cardreader.Components;

[RegisterComponent]
public sealed partial class CardreaderComponent : Component
{

    [DataField("cooldown")]
    public float Cooldown = 1f; // Секунды

    [ViewVariables]
    public TimeSpan LastActivationTime;

    [DataField]
    public ProtoId<SourcePortPrototype> Trigger = "Output";

    [DataField("soundAccept"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundAccept = new SoundPathSpecifier("/Audio/SD/Cardreader/entity_cardlock_granted.ogg");

    [DataField("soundDeny"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundDeny = new SoundPathSpecifier("/Audio/SD/Cardreader/entity_cardlock_denied.ogg");

    [DataField("requiredAccess"), ViewVariables(VVAccess.ReadWrite)]
    public int RequiredAccess = 1;

    [DataField("allowHigherAccess"), ViewVariables(VVAccess.ReadWrite)]
    public bool AllowHigherAccess = false;

}
