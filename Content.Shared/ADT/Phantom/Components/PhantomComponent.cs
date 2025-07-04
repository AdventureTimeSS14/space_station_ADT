using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Eui;
using Content.Shared.ADT.Hallucinations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Containers;
using Robust.Shared.Audio;
using Content.Shared.Alert;

namespace Content.Shared.ADT.Phantom.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
public sealed partial class PhantomComponent : Component
{

    #region Actions

    public EntProtoId PhantomHauntAction = "ActionPhantomHaunt";

    [AutoNetworkedField]
    public EntityUid? PhantomHauntActionEntity;

    public EntProtoId PhantomMakeVesselAction = "ActionPhantomMakeVessel";

    [AutoNetworkedField]
    public EntityUid? PhantomMakeVesselActionEntity;

    public EntProtoId PhantomHauntVesselAction = "ActionPhantomHauntVessel";

    [AutoNetworkedField]
    public EntityUid? PhantomHauntVesselActionEntity;

    public EntProtoId PhantomStyleAction = "ActionPhantomSelectStyle";

    [AutoNetworkedField]
    public EntityUid? PhantomStyleActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public string CurrentStyle = "PhantomStyleMove";

    public List<EntityUid?> CurrentActions = new();

    public List<EntityUid> TempContainedActions = new();
    #endregion

    #region Toggleable Actions
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsCorporeal = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> Toggleables = new();

    public EntityUid Claws = new();

    #endregion

    #region Sounds
    public SoundSpecifier SpeechSound = new SoundCollectionSpecifier("PhantomSpeech")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier PsychoSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/psycho.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/helping-hand-accept.ogg");
    #endregion

    #region Alerts
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> VesselCountAlert = "PhantomVessels";

    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> EssenceCountAlert = "PhantomEssense";

    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> HauntedAlert = "PhantomStopHaunt";

    #endregion

    /// <summary>
    /// The total amount of Essence the phantom has. Functions
    /// as health and is regenerated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Essence = 50;

    /// <summary>
    /// Prototype to spawn when the entity dies.
    /// </summary>
    [DataField("spawnOnDeathPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnOnDeathPrototype = "ADTPhantomEctoplasm";

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssence")]
    public FixedPoint2 EssenceRegenCap = 75;

    /// <summary>
    /// The amount of essence passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public FixedPoint2 EssencePerSecond = 0.5f;

    /// <summary>
    /// Damage phantom get while in church every second
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public FixedPoint2 ChurchDamage = -5f;

    public float Accumulator = 0;

    /// <summary>
    /// Entity that currently is haunted
    /// </summary>
    public EntityUid Holder = new EntityUid();

    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasHaunted = false;

    public List<EntityUid> Vessels = new List<EntityUid>();

    public List<EntityUid> CursedVessels = new List<EntityUid>();

    [DataField]
    public int VesselsStrandCap = 10;

    [DataField]
    public int MakeVesselDuration = 4;

    [DataField]
    public int PuppeterDuration = 16;

    [DataField]
    public float HolyDamageMultiplier = 5f;

    [DataField]
    public float InjuryDamage = 40f;

    [DataField]
    public float RegenerateBurnHealAmount = -40f;

    [DataField]
    public float RegenerateBruteHealAmount = -25f;

    [DataField]
    public float BlindingBleed = 6f;

    [DataField]
    public TimeSpan BlindingTime = TimeSpan.FromSeconds(4);

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Portal1 = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Portal2 = new();

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string PortalPrototype = "ADTPhantomPortal";

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<HallucinationsPrototype>))]
    public string HallucinationsPrototype = "Phantom";

    [ViewVariables(VVAccess.ReadWrite)]
    public Container HelpingHand = default!;

    [DataField]
    public int HelpingHandDuration = 10;
    public int HelpingHandTimer = 0;

    public int SpeechTimer = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid TransferringEntity = new EntityUid();

    [DataField("monsters", required: true)]
    public List<string> NightmareMonsters;

    [DataField]
    public int UsedActionsBeforeEctoplasm = 0;

    [DataField]
    public bool IgnoreLevels = false;

    [DataField]
    public int MaxReachedLevel = 0;
    #region Finale
    [DataField]
    public bool FinalAbilityUsed = false;

    [DataField]
    public bool CanHaunt = true;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool NightmareStarted = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool TyranyStarted = false;
    #endregion
    #region Visualizer
    [DataField("state")]
    public string State = "phantom";
    [DataField("hauntState")]
    public string HauntingState = "haunt";
    [DataField("corporealState")]
    public string CorporealState = "corporeal";
    [DataField("stunnedState")]
    public string StunnedState = "stunned";
    #endregion

}
