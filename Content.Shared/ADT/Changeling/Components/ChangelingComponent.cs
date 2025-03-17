using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Content.Shared.DoAfter;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ChangelingComponent : Component
{
    #region Chemicals
    /// <summary>
    /// The amount of chemicals the ling has.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float Chemicals = 20f;

    public float Accumulator = 0f;

    /// <summary>
    /// The amount of chemicals passively generated per second
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float ChemicalsPerSecond = 0.5f;

    /// <summary>
    /// The lings's current max amount of chemicals.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float MaxChemicals = 75f;
    #endregion

    #region DNA
    /// <summary>
    /// The maximum amount of DNA strands a ling can have at one time
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int DNAStrandCap = 7;

    /// <summary>
    /// List of stolen DNA
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<PolymorphHumanoidData> StoredDNA = new List<PolymorphHumanoidData>();
    #endregion

    #region Audio
    /// <summary>
    /// Sound that plays when the ling uses the regenerate ability.
    /// </summary>
    public SoundSpecifier? SoundRegenerate = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// </summary>
    /// Flesh sound
    /// </summary>
    public SoundSpecifier? SoundFlesh = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier? SoundResonant = new SoundPathSpecifier("/Audio/ADT/resonant.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    #endregion

    #region Basic Actions

    public List<EntProtoId> ActionIds = new()
    {
        "ActionChangelingEvolutionMenu",
        "ActionLingRegenerate",
        "ActionChangelingAbsorb",
        "ActionChangelingTransform",
        "ActionLingStingExtract",
        "ActionStasisDeath"
    };

    [AutoNetworkedField]
    public Dictionary<string, EntityUid> BoughtActions = new();

    [AutoNetworkedField]
    public Dictionary<string, EntityUid> BasicTransferredActions = new();

    public bool GainedActions = false;
    #endregion

    #region DNA Absorb Ability
    /// <summary>
    /// How long an absorb stage takes, in seconds.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int AbsorbDuration = 10;

    /// <summary>
    /// The stage of absorbing that the changeling is on. Maximum of 2 stages.
    /// </summary>
    public int AbsorbStage = 0;

    /// <summary>
    /// The amount of evolution points the changeling gains when they absorb another changeling.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AbsorbedChangelingPointsAmount = 5.0f;

    /// <summary>
    /// The amount of evolution points the changeling gains when they absorb somebody.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AbsorbedMobPointsAmount = 2.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public DoAfterId? DoAfter;
    #endregion

    #region Ability entities
    [AutoNetworkedField]
    public EntityUid? BladeEntity;
    [AutoNetworkedField]
    public EntityUid? ArmaceEntity;

    [AutoNetworkedField]
    public EntityUid? ShieldEntity;
    #endregion

    #region Chitinous Armor Ability
    /// <summary>
    /// The amount of chemical regeneration is reduced when the ling armor is active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float LingArmorRegenCost = 0.125f;

    /// <summary>
    /// If the ling has the armor on or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LingArmorActive = false;
    #endregion

    #region Chameleon Skin Ability
    /// <summary>
    /// If the ling has chameleon skin active or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ChameleonSkinActive = false;
    #endregion

    #region Stasis Death Ability

    [ViewVariables(VVAccess.ReadWrite)]
    public bool StasisDeathActive = false;

    #endregion

    #region Muscles Ability

    [ViewVariables(VVAccess.ReadWrite)]
    public bool MusclesActive = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MusclesModifier = 1.4f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float MusclesStaminaDamage = 3f;

    public TimeSpan NextMusclesDamage = TimeSpan.Zero;
    #endregion

    #region Lesser Form Ability
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LesserFormActive = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public string LesserFormMob = "ADTChangelingLesserForm";
    #endregion

    #region Other Abilities
    [ViewVariables(VVAccess.ReadWrite)]
    public bool LastResortUsed = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool DigitalCamouflageActive = false;
    #endregion

    #region Other
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanRefresh = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public float AbsorbedDnaModifier = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public int DNAStolen = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public int ChangelingsAbsorbed = 0;

    public bool CanTransform
    {
        get => !ArmaceEntity.HasValue &&
               !BladeEntity.HasValue &&
               !ShieldEntity.HasValue &&
               !ChameleonSkinActive &&
               !DigitalCamouflageActive &&
               !MusclesActive;
    }
    #endregion
}
