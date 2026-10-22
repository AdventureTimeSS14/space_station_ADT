using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.VoiceOfGod;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTVoiceOfGodComponent : Component
{
    [DataField]
    public EntProtoId ActionProto = "ActionADTVoiceOfGod";

    [ViewVariables]
    public EntityUid? ActionUid;

    [DataField]
    public TimeSpan InsertDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public float Range = 8f;

    [DataField]
    public float BaseMultiplier = 1f;

    [DataField]
    public List<ADTVoiceOfGodPowerModifier> PowerModifiers = new();

    [DataField]
    public TimeSpan CooldownStun = TimeSpan.FromSeconds(120);

    [DataField]
    public TimeSpan CooldownDamage = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan CooldownMeme = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan CooldownNone = TimeSpan.FromSeconds(15);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/Colossus/invoke_general.ogg", AudioParams.Default.WithVolume(8f).WithMaxDistance(20f));

    [DataField]
    public Color SpeechColor = Color.FromHex("#80282A");

    [ViewVariables]
    public TimeSpan NextCommand;

    [ViewVariables]
    public bool Active;
}

[RegisterComponent]
public sealed partial class ADTVoiceOfGodSpeakerComponent : Component
{
    [ViewVariables]
    public EntityUid Organ;
}

public sealed partial class ADTVoiceOfGodActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ADTVoiceOfGodInsertDoAfterEvent : SimpleDoAfterEvent;
