using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.SignalingLoudspeak;

[RegisterComponent]
public sealed partial class SignalingLoudspeakComponent : Component
{

    #region Sound

    [DataField]
    public SoundSpecifier? SoundLong = new SoundPathSpecifier("/Audio/ADT/Misc/police-sirenss.ogg");

    [DataField]
    public SoundSpecifier? SoundShort = new SoundPathSpecifier("/Audio/ADT/Misc/sgu.ogg");


    [DataField]
    public SoundSpecifier? SoundSpeak = new SoundPathSpecifier("/Audio/ADT/Misc/sgu_speak.ogg");

    #endregion

    #region Setting

    [DataField]
    public SelectiveSignaling SelectedModeSound = SelectiveSignaling.Long;

    [DataField]
    public float AudioVolume = 9f;

    [DataField]
    public float AudioMaxDistance = 20f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("broadcastChannel", customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
    public string BroadcastChannel = SharedChatSystem.CommonChannel;

    #endregion

    [DataField("microphoneEnabled")]
    public bool MicrophoneEnabled;

    [DataField]
    public EntityUid? PlayingStream;
}

[Flags]
public enum SelectiveSignaling : byte
{
    Invalid = 0,
    Short = 1 << 0,
    Long = 1 << 1,
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
