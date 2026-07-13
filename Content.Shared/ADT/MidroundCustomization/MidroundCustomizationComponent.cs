using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Server.ADT.MidroundCustomization;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MidroundCustomizationComponent : Component
{
    [DataField]
    public DoAfterId? DoAfter;

    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/Machines/beep.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionMidroundCustomization";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public HashSet<HumanoidVisualLayers> AllowedLayers = [];
}