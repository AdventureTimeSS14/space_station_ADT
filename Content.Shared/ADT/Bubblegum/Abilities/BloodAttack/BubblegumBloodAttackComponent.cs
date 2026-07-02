using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum.Abilities;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumBloodAttackComponent : Component
{
    [DataField]
    public TimeSpan NextAttemptAt;

    [DataField]
    public TimeSpan AttemptInterval = TimeSpan.FromSeconds(4);

    [DataField]
    public float SearchRange = 8f;

    [DataField]
    public float PuddleCheckRange = 0.4f;

    [DataField]
    public float SmackDamage = 10f;

    [DataField]
    public float SmackDamageEnraged = 20f;

    [DataField]
    public float GrabConsciousChance = 0.1f;

    [DataField]
    public TimeSpan SmackHitDelay = TimeSpan.FromSeconds(0.4);

    [DataField]
    public TimeSpan GrabHitDelay = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan DevourDelay = TimeSpan.FromSeconds(0.2);

    [DataField]
    public SoundSpecifier HitSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/demon_attack1.ogg");

    [DataField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/enter_blood.ogg");

    [DataField]
    public EntProtoId LeftSmackProto = "ADTBubblegumHandLeftSmack";

    [DataField]
    public EntProtoId RightSmackProto = "ADTBubblegumHandRightSmack";

    [DataField]
    public EntProtoId LeftPawProto = "ADTBubblegumHandLeftPaw";

    [DataField]
    public EntProtoId LeftThumbProto = "ADTBubblegumHandLeftThumb";

    [DataField]
    public EntProtoId RightPawProto = "ADTBubblegumHandRightPaw";

    [DataField]
    public EntProtoId RightThumbProto = "ADTBubblegumHandRightThumb";
}
