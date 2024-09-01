using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BloodCough;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class BloodCoughComponent : Component
{
    // public TimeSpan NextSecond = TimeSpan.Zero;

    // [DataField("emoteTimer"), ViewVariables(VVAccess.ReadWrite)]
    // public int EmoteTimerRead = 9;

    // [DataField("emoteMessage")]
    // public string? PostingMessageEmote = "Кхе";

    // [DataField("randomIntervalEmote"), ViewVariables(VVAccess.ReadWrite)]
    // public bool RandomIntervalEmote = false;
}
