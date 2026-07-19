using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumLootComponent : Component
{
    [DataField]
    public TimeSpan DespawnDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId ChestProto = "ADTCrateBubblegumLoot";

    [DataField]
    public Dictionary<EntProtoId, int> RandomAmountLoot = [];

    [DataField]
    public List<EntProtoId> RandomLoot = [];

    [DataField]
    public List<EntProtoId> GuaranteedLoot = [];

    [DataField]
    public bool LootDropped;

    [DataField]
    public TimeSpan? DespawnAt;

    [DataField]
    public bool SequenceStarted;

    [DataField]
    public bool SoulReleased;

    [DataField]
    public TimeSpan ImplosionLead = TimeSpan.FromSeconds(1.5);

    [DataField]
    public EntProtoId DemonSoulProto = "ADTBubblegumDemonSoul";

    [DataField]
    public TimeSpan NextBloodBeatAt;

    [DataField]
    public TimeSpan BloodBeatInterval = TimeSpan.FromSeconds(0.6);

    [DataField]
    public float BloodRingRadius = 2.5f;

    [DataField]
    public int BloodRingCount = 8;

    [DataField]
    public float ShakeRange = 14f;

    [DataField]
    public float ShakeStrength = 5f;

    [DataField]
    public TimeSpan FlashDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public float ImpactSlowTo = 0.4f;

    [DataField]
    public TimeSpan ImpactSlowDuration = TimeSpan.FromSeconds(0.7);

    [DataField]
    public EntProtoId DeathGlowProto = "ADTBubblegumDeathGlow";

    [DataField]
    public EntProtoId DeathFlashProto = "ADTBubblegumDeathFlash";

    [DataField]
    public EntProtoId ChestGlowProto = "ADTBubblegumChestGlow";

    [DataField]
    public SoundSpecifier DeathBoomSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/meteorimpact.ogg");

    [DataField]
    public SoundSpecifier DeathRoarSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/demon_attack1.ogg");

    [DataField]
    public SoundSpecifier DissolveSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/enter_blood.ogg");

    [DataField]
    public SoundSpecifier ChestRewardSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/exit_blood.ogg");

    [DataField]
    public SoundSpecifier DeathThemeSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/e1m1.ogg");

    [DataField]
    public SoundSpecifier ImplosionSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/glassbr1.ogg");
}
