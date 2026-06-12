using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Bubblegum;

[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumComponent : Component
{
    [DataField]
    public float AngerModifier;

    [DataField]
    public float HealingReceivedFromEnrage;

    [DataField]
    public float MaxHealingFromEnrage = 500f;

    [DataField]
    public float EnrageHeal = 75f;

    [DataField]
    public TimeSpan BaseEnrageDuration = TimeSpan.FromSeconds(7);

    [DataField]
    public TimeSpan EnrageEndsAt;

    [DataField]
    public TimeSpan NextEnrageAvailableAt;

    [DataField]
    public float EnragedSpeedMultiplier = 1.5f;

    [DataField]
    public Color EnragedColor = Color.FromHex("#950A0A");

    [DataField]
    public EntProtoId BloodPrototype = "ADTPuddleBloodBubblegum";

    [DataField]
    public EntProtoId ThickBloodPrototype = "ADTPuddleBloodBubblegumThick";

    [DataField]
    public EntProtoId DecoyPrototype = "ADTBubblegumDecoy";

    [DataField]
    public float ThickBloodOnDamageChance = 0.25f;

    [DataField]
    public SoundSpecifier RangedDeflectSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/ric1.ogg");

    [DataField]
    public SoundSpecifier StepSound = new SoundPathSpecifier("/Audio/ADT/Bubblegum/meteorimpact.ogg");

    [DataField]
    public TimeSpan StepSoundCooldown = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan LastStepSound;

    [DataField]
    public TimeSpan NextSpeechTime = TimeSpan.Zero;

    [DataField]
    public MapCoordinates? LastBloodPosition;

    [DataField]
    public float BloodStepDistance = 0.7f;

    [DataField]
    public bool InSmashPhase;

    [DataField]
    public List<EntProtoId> Actions = new()
    {
        "ActionADTBubblegumTripleCharge",
        "ActionADTBubblegumHallucinationCharge",
        "ActionADTBubblegumSurround",
        "ActionADTBubblegumBloodWarp",
        "ActionADTBubblegumSummonNarsi",
    };

    [DataField]
    public TimeSpan AbilityInterval = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan NextAbilityAt;
}

[Serializable, NetSerializable]
public enum BubblegumVisuals : byte
{
    Enraged,
    Charging,
}
