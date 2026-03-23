using Content.Shared.Chat;
using Content.Shared.Radio.EntitySystems;
using Robust.Shared.Audio;
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

    /// Start ADT Tweak
    /// <summary>
    /// The sound effect played when radio receive message
    /// </summary>
    [DataField]
    public SoundSpecifier SoundOnReceive = new SoundPathSpecifier("/Audio/ADT/Effects/silence.ogg");

    /// <summary>
    /// speaks normally when true whispers when false
    /// </summary>
    [DataField]
    public bool SpeakNormally;

    /// <summary>
    /// Does the radio need to be on a power grid to work?
    /// </summary>
    [DataField]
    public bool PowerRequired;
    /// End ADT Tweak
}