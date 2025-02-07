using Robust.Shared.GameStates;

namespace Content.Server.ADT.AutoPostingChat;

[RegisterComponent]
public sealed partial class AutoSpeakPostingChatComponent : Component
{

    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField("speakTimer"), ViewVariables(VVAccess.ReadWrite)]
    public int SpeakTimerRead = 10;

    [DataField("speakMessage")]
    public string? PostingMessageSpeak = default;

    [DataField("randomIntervalSpeak"), ViewVariables(VVAccess.ReadWrite)]
    public bool RandomIntervalSpeak = false;

    [DataField("max"), ViewVariables(VVAccess.ReadWrite)]
    public int IntervalRandomSpeakMax = 30;

    [DataField("min"), ViewVariables(VVAccess.ReadWrite)]
    public int IntervalRandomSpeakMin = 2;

}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
