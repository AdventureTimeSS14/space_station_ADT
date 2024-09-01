using Robust.Shared.GameStates;

namespace Content.Shared.ADT.AutoPostingChat;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class AutoEmotePostingChatComponent : Component
{

    public TimeSpan NextSecond = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    public int EmoteTimerRead = 9;

    [DataField("emoteMessage")]
    public string? PostingMessageEmote = "Кхе";
}
