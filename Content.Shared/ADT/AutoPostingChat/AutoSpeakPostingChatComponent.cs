using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.AutoPostingChat;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class AutoSpeakPostingChatComponent : Component
{

    public TimeSpan NextSecond = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public int SpeakTimerRead = 10;

    [ViewVariables(VVAccess.ReadWrite)]
    public int EmoteTimerRead = 9;

    [DataField("speakMessage")]
    public string? PostingMessageSpeak = "Вульп-вульп!";


    [DataField("emoteMessage")]
    public string? PostingMessageEmote = "Кхе";



}
