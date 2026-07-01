using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.AutoPostingChat;

[RegisterComponent]
public sealed partial class AutoEmotePostingChatComponent : Component
{
    [DataField("emoteTimer"), ViewVariables(VVAccess.ReadWrite)]
    public int EmoteTimerRead = 9;

    [DataField("emoteMessage")]
    public string? PostingMessageEmote = default;

    [DataField("randomIntervalEmote"), ViewVariables(VVAccess.ReadWrite)]
    public bool RandomIntervalEmote = false;

    [DataField("max"), ViewVariables(VVAccess.ReadWrite)]
    public int IntervalRandomEmoteMax = 30;

    [DataField("min"), ViewVariables(VVAccess.ReadWrite)]
    public int IntervalRandomEmoteMin = 2;

    /// <summary>
    /// The time at which the next emote will be sent.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextFire = TimeSpan.Zero;
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
