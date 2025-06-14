using Content.Server.Radio.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Content.Server.Chat.Systems; // ADT Radio Update
using Robust.Shared.Audio; // ADT Radio Update

namespace Content.Server.Radio.Components;

/// <summary>
///     Listens for radio messages and relays them to local chat.
/// </summary>
[RegisterComponent]
[Access(typeof(RadioDeviceSystem))]
public sealed partial class RadioSpeakerComponent : Component
{
    /// <summary>
    /// Whether or not interacting with this entity
    /// toggles it on or off.
    /// </summary>
    [DataField("toggleOnInteract")]
    public bool ToggleOnInteract = true;

    [DataField("channels", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<RadioChannelPrototype>))]
    public HashSet<string> Channels = new () { SharedChatSystem.CommonChannel };

    [DataField("enabled")]
    public bool Enabled;
    /// Start ADT Tweak
    [DataField("speechMode")]
    public InGameICChatType SpeechMode = InGameICChatType.Whisper;

    /// <summary>
    /// The sound effect played when radio receive message
    /// </summary>
    [DataField]
    public SoundSpecifier SoundOnReceive = new SoundPathSpecifier("/Audio/ADT/Effects/silence.ogg");
    /// End ADT Tweak
}
