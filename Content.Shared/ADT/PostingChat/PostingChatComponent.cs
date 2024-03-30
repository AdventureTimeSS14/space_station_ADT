using Robust.Shared.Audio;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.PostingChat;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class PostingChatComponent : Component
{
    /// <summary>
    /// timings for giggles and knocks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DamageGiggleCooldown = TimeSpan.FromSeconds(2);

    //[ViewVariables(VVAccess.ReadWrite)]
    //public float KnockChance = 0.05f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float GiggleRandomChance = 0.3f;

    [DataField("speakMessage")] 
    public string? PostingMessageSpeak = "Вульп-вульп!";

    [DataField("emoteMessage")]
    public string? PostingMessageEmote = "Кхе";
}
