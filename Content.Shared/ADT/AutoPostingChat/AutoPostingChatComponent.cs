using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.AutoPostingChat;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class AutoPostingChatComponent : Component
{

    public TimeSpan NextSecond = TimeSpan.MaxValue;

    [ViewVariables(VVAccess.ReadWrite)]
    public int SpeakTimerRead = 80;

    [ViewVariables(VVAccess.ReadWrite)]
    public int EmoteTimerRead = 9;

    [DataField("speakMessage")]
    public string? PostingMessageSpeak = "Вульп-вульп!";


    [DataField("emoteMessage")]
    public string? PostingMessageEmote = "Кхе";



}
