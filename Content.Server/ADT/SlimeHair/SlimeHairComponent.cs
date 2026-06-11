using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.ADT.SlimeHair;

[RegisterComponent]
public sealed partial class SlimeHairComponent : Component
{
    [DataField]
    public DoAfterId? DoAfter;

    [DataField]
    public EntityUid? Target;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(2);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(2);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(2);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/ADT/slime-hair.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField("hairAction")]
    public EntProtoId Action = "ActionSlimeHair";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

}
