using Robust.Shared.Audio;
using Content.Shared.Polymorph;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class ChangelingComponent : Component
{
    /// <summary>
    /// The amount of chemicals the ling has.
    /// </summary>
    [DataField]
    public float Chemicals = 20f;

    [DataField]
    public float Accumulator = 0f;

    /// <summary>
    /// The amount of chemicals passively generated per second
    /// </summary>
    [DataField]
    public float ChemicalsPerSecond = 0.5f;

    /// <summary>
    /// The lings's current max amount of chemicals.
    /// </summary>
    [DataField]
    public float MaxChemicals = 75f;

    /// <summary>
    /// The maximum amount of DNA strands a ling can have at one time
    /// </summary>
    [DataField]
    public int DNAStrandCap = 7;

    /// <summary>
    /// List of stolen DNA
    /// </summary>
    [DataField]
    public List<PolymorphHumanoidData> StoredDNA = new List<PolymorphHumanoidData>();

    /// <summary>
    /// The DNA index that the changeling currently has selected
    /// </summary>
    [DataField]
    public int SelectedDNA = 0;

    /// </summary>
    /// Flesh sound
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundFlesh = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// </summary>
    /// Flesh sound
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundFleshQuiet = new SoundPathSpecifier("/Audio/Effects/blobattack.ogg")
    {
        Params = AudioParams.Default.WithVolume(-1f),
    };

    [DataField]
    public SoundSpecifier? SoundResonant = new SoundPathSpecifier("/Audio/ADT/resonant.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Blind sting duration
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BlindStingDuration = TimeSpan.FromSeconds(18);

    /// <summary>
    /// Refresh ability
    /// </summary>
    [DataField]
    public bool CanRefresh = false;

    #region Actions

    [DataField]
    public EntProtoId ChangelingEvolutionMenuAction = "ActionChangelingEvolutionMenu";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingEvolutionMenuActionEntity;

    [DataField]
    public EntProtoId ChangelingRegenAction = "ActionLingRegenerate";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingRegenActionEntity;

    [DataField]
    public EntProtoId ChangelingAbsorbAction = "ActionChangelingAbsorb";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingAbsorbActionEntity;

    [DataField]
    public EntProtoId ChangelingDNACycleAction = "ActionChangelingCycleDNA";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingDNACycleActionEntity;

    [DataField]
    public EntProtoId ChangelingRefreshAction = "ActionLingRefresh";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingRefreshActionEntity;

    [DataField]
    public EntProtoId ChangelingDNAStingAction = "ActionLingStingExtract";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingDNAStingActionEntity;

    [DataField]
    public EntProtoId ChangelingLesserFormAction = "ActionLingLesserForm";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingLesserFormActionEntity;

    [DataField]
    public EntProtoId ChangelingLastResortAction = "ActionLingLastResort";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingLastResortActionEntity;

    [DataField]
    public EntProtoId ChangelingStasisDeathAction = "ActionStasisDeath";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingStasisDeathActionEntity;
    #endregion

    #region Chemical Costs
    [DataField]
    public float ChemicalsCostFree = 0;

    [DataField]
    public float ChemicalsCostFive = -5f;

    [DataField]
    public float ChemicalsCostTen = -10f;

    [DataField]
    public float ChemicalsCostFifteen = -15f;

    [DataField]
    public float ChemicalsCostTwenty = -20f;

    [DataField]
    public float ChemicalsCostTwentyFive = -25f;

    [DataField]
    public float ChemicalsCostFifty = -50f;
    #endregion

    #region DNA Absorb Ability
    /// <summary>
    /// How long an absorb stage takes, in seconds.
    /// </summary>
    [DataField]
    public int AbsorbDuration = 10;

    /// <summary>
    /// The stage of absorbing that the changeling is on. Maximum of 2 stages.
    /// </summary>
    [DataField]
    public int AbsorbStage = 0;

    /// <summary>
    /// The amount of genetic damage the target gains when they're absorbed.
    /// </summary>
    [DataField]
    public float AbsorbGeneticDmg = 200.0f;

    /// <summary>
    /// The amount of evolution points the changeling gains when they absorb another changeling.
    /// </summary>
    [DataField]
    public float AbsorbedChangelingPointsAmount = 5.0f;

    /// <summary>
    /// The amount of evolution points the changeling gains when they absorb somebody.
    /// </summary>
    [DataField]
    public float AbsorbedMobPointsAmount = 2.0f;

    [DataField]
    public float AbsorbedDnaModifier = 0f;

    #endregion

    #region Regenerate Ability
    /// <summary>
    /// The amount of burn damage is healed when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBurnHealAmount = -100f;

    /// <summary>
    /// The amount of brute damage is healed when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBruteHealAmount = -125f;

    /// <summary>
    /// The amount of blood volume that is gained when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBloodVolumeHealAmount = 1000f;

    /// <summary>
    /// The amount of bleeding that is reduced when the regenerate ability is sucesssfully used.
    /// </summary>
    [DataField]
    public float RegenerateBleedReduceAmount = -1000f;

    /// <summary>
    /// Sound that plays when the ling uses the regenerate ability.
    /// </summary>
    [DataField]
    public SoundSpecifier? SoundRegenerate = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    #endregion

    #region Armblade Ability
    /// <summary>
    /// If the ling has an active armblade or not.
    /// </summary>
    [DataField]
    public bool ArmBladeActive = false;

    [DataField, AutoNetworkedField]
    public EntityUid? BladeEntity;

    #endregion

    #region Chitinous Armor Ability
    /// <summary>
    /// The amount of chemical regeneration is reduced when the ling armor is active.
    /// </summary>
    [DataField]
    public float LingArmorRegenCost = 0.125f;

    /// <summary>
    /// If the ling has the armor on or not.
    /// </summary>
    [DataField]
    public bool LingArmorActive = false;
    #endregion

    #region Chameleon Skin Ability
    /// <summary>
    /// If the ling has chameleon skin active or not.
    /// </summary>
    [DataField]
    public bool ChameleonSkinActive = false;

    /// <summary>
    /// How fast the changeling will turn invisible from standing still when using chameleon skin.
    /// </summary>
    [DataField]
    public float ChameleonSkinPassiveVisibilityRate = -0.10f;

    /// <summary>
    /// How fast the changeling will turn visible from movement when using chameleon skin.
    /// </summary>
    [DataField]
    public float ChameleonSkinMovementVisibilityRate = 0.60f;
    #endregion

    #region Dissonant Shriek Ability
    /// <summary>
    /// Range of the Dissonant Shriek's EMP in tiles.
    /// </summary>
    [DataField]
    public float DissonantShriekEmpRange = 5f;

    /// <summary>
    /// Power consumed from batteries by the Dissonant Shriek's EMP
    /// </summary>
    [DataField]
    public float DissonantShriekEmpConsumption = 50000f;

    /// <summary>
    /// How long the Dissonant Shriek's EMP effects last for
    /// </summary>
    [DataField]
    public float DissonantShriekEmpDuration = 12f;
    #endregion

    #region Stasis Death Ability

    [DataField]
    public float StasisDeathDamageAmount = 10000f;    /// Damage gain to die

    [DataField]
    public bool StasisDeathActive = false;      /// Is ling dead or alive

    #endregion

    #region Muscles Ability

    [DataField]
    public bool MusclesActive = false;

    [DataField]
    public float MusclesModifier = 1.4f;

    [DataField]
    public float MusclesStaminaDamage = 3f;

    [ViewVariables]
    public TimeSpan NextMusclesDamage = TimeSpan.Zero;
    #endregion

    #region Changeling Chemicals

    [DataField("chemicalMorphine", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string ChemicalMorphine = "ADTMMorphine";

    [DataField("chemicalTranex", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string ChemicalTranex = "TranexamicAcid";

    [DataField("chemicalOmnizine", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string ChemicalOmni = "Omnizine";

    [DataField("chemicalMute", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string ChemicalMute = "MuteToxin";

    [DataField("chemicalDrug", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string ChemicalSpaceDrugs = "LingDrugs";

    #endregion

    #region Changeling Chemicals Amount

    [ViewVariables(VVAccess.ReadWrite), DataField("adrenalineAmount")]
    public float AdrenalineAmount = 5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("muteAmount")]
    public float MuteAmount = 20f;

    [ViewVariables(VVAccess.ReadWrite), DataField("drugAmount")]
    public float SpaceDrugsAmount = 50f;

    #endregion

    #region Lesser Form Ability

    [DataField]
    public bool LesserFormActive = false;

    [DataField]
    public string LesserFormMob = "ChangelingLesserForm";


    #endregion

    #region Armshield Ability
    /// <summary>
    /// If the ling has an active armblade or not.
    /// </summary>
    [DataField]
    public bool ArmShieldActive = false;

    [DataField, AutoNetworkedField]
    public EntityUid? ShieldEntity;

    #endregion

    [DataField]
    public float GibDamage = 5000f;

    [DataField]
    public bool EggedBody = false;

    [DataField]
    public bool EggsReady = false;

    [DataField]
    public int BiodegradeDuration = 3;

    [DataField]
    public string HiveMind = "ChangelingCollectiveMind";

    [DataField]
    public bool ShowName = false;

    [DataField]
    public bool ShowRank = true;

    [DataField]
    public string RankName = "collective-mind-ling-rank";



}
