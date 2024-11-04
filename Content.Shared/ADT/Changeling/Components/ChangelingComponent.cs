using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.DoAfter;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ChangelingComponent : Component
{
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

    /// <summary>
    /// The DNA index that the changeling currently has selected
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int SelectedDNA = 0;

    /// </summary>
    /// Flesh sound
    /// </summary>
    public SoundSpecifier? SoundFlesh = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// </summary>
    /// Flesh sound
    /// </summary>
    public SoundSpecifier? SoundFleshQuiet = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    public SoundSpecifier? SoundResonant = new SoundPathSpecifier("/Audio/ADT/resonant.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Blind sting duration
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BlindStingDuration = TimeSpan.FromSeconds(18);

    /// <summary>
    /// Refresh ability
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanRefresh = false;

    #region Actions
    public EntProtoId ChangelingEvolutionMenuAction = "ActionChangelingEvolutionMenu";

    [AutoNetworkedField]
    public EntityUid? ChangelingEvolutionMenuActionEntity;

    public EntProtoId ChangelingRegenAction = "ActionLingRegenerate";

    [AutoNetworkedField]
    public EntityUid? ChangelingRegenActionEntity;

    public EntProtoId ChangelingAbsorbAction = "ActionChangelingAbsorb";

    [AutoNetworkedField]
    public EntityUid? ChangelingAbsorbActionEntity;

    public EntProtoId ChangelingDNACycleAction = "ActionChangelingCycleDNA";

    [AutoNetworkedField]
    public EntityUid? ChangelingDNACycleActionEntity;

    public EntProtoId ChangelingDNAStingAction = "ActionLingStingExtract";

    [AutoNetworkedField]
    public EntityUid? ChangelingDNAStingActionEntity;

    public EntProtoId ChangelingStasisDeathAction = "ActionStasisDeath";

    [AutoNetworkedField]
    public EntityUid? ChangelingStasisDeathActionEntity;

    public List<EntityUid?> BoughtActions = new();

    public List<EntityUid?> BasicTransferredActions = new();
    #endregion

    #region Chemical Costs
    public float ChemicalsCostFree = 0;
    public float ChemicalsCostFive = -5f;
    public float ChemicalsCostTen = -10f;
    public float ChemicalsCostFifteen = -15f;
    public float ChemicalsCostTwenty = -20f;
    public float ChemicalsCostTwentyFive = -25f;
    public float ChemicalsCostFifty = -50f;
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
    /// The amount of genetic damage the target gains when they're absorbed.
    /// </summary>
    public float AbsorbGeneticDmg = 200.0f;

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
    public float AbsorbedDnaModifier = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    public DoAfterId? DoAfter;
    #endregion

    #region Regenerate Ability
    /// <summary>
    /// The amount of burn damage is healed when the regenerate ability is sucesssfully used.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RegenerateBurnHealAmount = -100f;

    /// <summary>
    /// The amount of brute damage is healed when the regenerate ability is sucesssfully used.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RegenerateBruteHealAmount = -125f;

    /// <summary>
    /// The amount of blood volume that is gained when the regenerate ability is sucesssfully used.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RegenerateBloodVolumeHealAmount = 1000f;

    /// <summary>
    /// The amount of bleeding that is reduced when the regenerate ability is sucesssfully used.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float RegenerateBleedReduceAmount = -1000f;

    /// <summary>
    /// Sound that plays when the ling uses the regenerate ability.
    /// </summary>
    public SoundSpecifier? SoundRegenerate = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    #endregion

    #region Armblade Ability
    /// <summary>
    /// If the ling has an active armblade or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ArmBladeActive = false;

    [AutoNetworkedField]
    public EntityUid? BladeEntity;

    #endregion

    #region Armace Ability
    /// <summary>
    /// If the ling has an active armace or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ArmaceActive = false;

    [AutoNetworkedField]
    public EntityUid? ArmaceEntity;
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

    /// <summary>
    /// How fast the changeling will turn invisible from standing still when using chameleon skin.
    /// </summary>
    public float ChameleonSkinPassiveVisibilityRate = -0.15f;

    /// <summary>
    /// How fast the changeling will turn visible from movement when using chameleon skin.
    /// </summary>
    public float ChameleonSkinMovementVisibilityRate = 0.60f;
    #endregion

    #region Dissonant Shriek Ability
    /// <summary>
    /// Range of the Dissonant Shriek's EMP in tiles.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float DissonantShriekEmpRange = 5f;

    /// <summary>
    /// Power consumed from batteries by the Dissonant Shriek's EMP
    /// </summary>
    public float DissonantShriekEmpConsumption = 50000f;

    /// <summary>
    /// How long the Dissonant Shriek's EMP effects last for
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float DissonantShriekEmpDuration = 12f;
    #endregion

    #region Stasis Death Ability

    [ViewVariables(VVAccess.ReadWrite)]
    public bool StasisDeathActive = false;      /// Is ling dead or alive

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

    #region Armshield Ability
    /// <summary>
    /// If the ling has an active armblade or not.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool ArmShieldActive = false;

    [AutoNetworkedField]
    public EntityUid? ShieldEntity;

    #endregion

    [ViewVariables(VVAccess.ReadWrite)]
    public int BiodegradeDuration = 3;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool LastResortUsed = false;
}
