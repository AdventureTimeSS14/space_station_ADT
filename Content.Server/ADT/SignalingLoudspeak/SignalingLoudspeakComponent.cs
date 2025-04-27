using Robust.Shared.Audio;

namespace Content.Server.ADT.SignalingLoudspeak;

[RegisterComponent]
public sealed partial class SignalingLoudspeakComponent : Component
{

    #region Sound

    [DataField]
    public SoundSpecifier? SoundLong = new SoundPathSpecifier("/Audio/ADT/Misc/police-sirenss.ogg");

    [DataField]
    public SoundSpecifier? SoundShort = new SoundPathSpecifier("/Audio/ADT/Misc/sgu.ogg");

    #endregion

    #region Setting

    [DataField]
    public SelectiveSignaling SelectedModeSound = SelectiveSignaling.Long;

    [DataField]
    public float AudioVolume = 7f;

    [DataField]
    public float AudioMaxDistance = 13f;

    #endregion



    // /// <summary>
    // ///     The continuous noise this item makes when it's activated (like an e-sword's hum).
    // /// </summary>
    // [DataField(required: true)]
    // public SoundSpecifier? ActiveSound;

    /// <summary>
    ///     Used when the item emits sound while active.
    /// </summary>
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
