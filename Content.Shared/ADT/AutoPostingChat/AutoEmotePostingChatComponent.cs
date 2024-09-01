using Robust.Shared.GameStates;

namespace Content.Shared.ADT.AutoPostingChat;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class AutoEmotePostingChatComponent : Component
{

    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField("emoteTimer"), ViewVariables(VVAccess.ReadWrite)]
    public int EmoteTimerRead = 9;

    [DataField("emoteMessage")]
    public string? PostingMessageEmote = "Кхе";

    [DataField("randomIntervalEmote"), ViewVariables(VVAccess.ReadWrite)]
    public bool RandomIntervalEmote = false;

    [DataField("max"), ViewVariables(VVAccess.ReadWrite)]
    public int IntervalRandomEmoteMax = 30;

    [DataField("min"), ViewVariables(VVAccess.ReadWrite)]
    public int IntervalRandomEmoteMin = 2;
}
