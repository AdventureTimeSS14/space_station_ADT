using Content.Shared.ADT.MartialArts;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Colossus;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTColossusComponent : Component
{
    [DataField]
    public EntProtoId BoltProto = "ADTColossusBolt";

    [DataField]
    public EntProtoId ShieldProto = "ADTColossusShield";

    [DataField]
    public EntProtoId RemainsProto = "Ash";

    [DataField]
    public float ShieldHitRadius = 0.4f;

    [DataField]
    public float BoltSpeed = 10f / 3.5f;

    [DataField]
    public float BoltRange = 50f;

    [DataField]
    public TimeSpan OpenFireCooldown = TimeSpan.FromSeconds(12);

    [DataField]
    public float SpiralChanceBase = 0.2f;

    [DataField]
    public float RandomShotsChance = 0.2f;

    [DataField]
    public float BlastChance = 0.7f;

    [DataField]
    public TimeSpan TelegraphDelay = TimeSpan.FromSeconds(2.5);

    [DataField]
    public int TelegraphRange = 10;

    [DataField]
    public Color TelegraphColor = Color.FromHex("#C80000");

    [DataField]
    public TimeSpan TelegraphFlashDuration = TimeSpan.FromSeconds(0.1);

    [DataField]
    public float TelegraphShakeStrength = 0.5f;

    [DataField]
    public TimeSpan CheeseCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public List<MartialArtsForms> CheeseForms = new()
    {
        MartialArtsForms.SleepingCarp,
    };

    [DataField]
    public float CheeseSpeedMultiplier = 10f / 3f;

    [DataField]
    public float DoubleSpiralHealthFraction = 1f / 3f;

    [DataField]
    public TimeSpan SpiralDelay = TimeSpan.FromSeconds(3.5);

    [DataField]
    public int SpiralShots = 80;

    [DataField]
    public int SpiralCounterStart = 8;

    [DataField]
    public int SpiralCounterMax = 16;

    [DataField]
    public float SpiralAngleStep = 22.5f;

    [DataField]
    public TimeSpan SpiralStepDelay = TimeSpan.FromSeconds(0.1);

    [DataField]
    public TimeSpan RandomShotsCooldown = TimeSpan.FromSeconds(3);

    [DataField]
    public int RandomShotsRange = 12;

    [DataField]
    public float RandomShotChance = 0.05f;

    [DataField]
    public TimeSpan BlastCooldown = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan BlastNoTelegraphDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public List<float> ShotgunAngles = new() { 12.5f, 7.5f, 2.5f, -2.5f, -7.5f, -12.5f };

    [DataField]
    public TimeSpan AlternatingCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan AlternatingDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public float FinalHealthDivisor = 9f;

    [DataField]
    public TimeSpan FinalCooldown = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan FinalEndCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan FinalDelay = TimeSpan.FromSeconds(3.5);

    [DataField]
    public int FinalBlasts = 20;

    [DataField]
    public int FinalCounterStart = 10;

    [DataField]
    public int FinalCounterBlastThreshold = 4;

    [DataField]
    public int FinalRandomShotsWaves = 3;

    [DataField]
    public int FinalCounterRandomShotsStep = 6;

    [DataField]
    public int FinalDirShotsWaves = 3;

    [DataField]
    public TimeSpan FinalDirShotsDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public SoundSpecifier ShotSound = new SoundPathSpecifier("/Audio/ADT/Colossus/invoke_general.ogg");

    [DataField]
    public SoundSpecifier TelegraphSound = new SoundPathSpecifier("/Audio/ADT/Colossus/narsie_attack.ogg");

    [DataField]
    public SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/Effects/demon_dies.ogg");

    [DataField]
    public float SpiralShotVolume = -14f;

    [DataField]
    public float VolleyVolume;

    [DataField]
    public float VolleyExtraRange = 2f;

    [DataField]
    public float RandomShotsExtraRange = 5f;

    [ViewVariables]
    public bool FinalAvailable = true;

    [ViewVariables]
    public bool Cheesing;

    [ViewVariables]
    public int AttackingSequences;
}

[Serializable, NetSerializable]
public enum ADTColossusVisuals : byte
{
    Attacking,
}
