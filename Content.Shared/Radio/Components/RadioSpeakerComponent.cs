<<<<<<< HEAD
using Content.Shared.Chat;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Audio;
=======
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Chat;
>>>>>>> upstreamwiz/master
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

/// <summary>
///     Listens for radio messages and relays them to local chat.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRadioDeviceSystem))]
public sealed partial class RadioSpeakerComponent : Component
{
    /// <summary>
    /// Whether or not interacting with this entity
    /// toggles it on or off.
    /// </summary>
    [DataField]
    public bool ToggleOnInteract = true;

    [DataField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new() { SharedChatSystem.CommonChannel };

    [DataField, AutoNetworkedField]
    public bool Enabled;
<<<<<<< HEAD

    /// Start ADT Tweak
    [DataField("speechMode")]
    public InGameICChatType SpeechMode = InGameICChatType.Whisper;

    /// <summary>
    /// The sound effect played when radio receive message
    /// </summary>
    [DataField]
    public SoundSpecifier SoundOnReceive = new SoundPathSpecifier("/Audio/ADT/Effects/silence.ogg");
    /// End ADT Tweak
=======
>>>>>>> upstreamwiz/master
}
