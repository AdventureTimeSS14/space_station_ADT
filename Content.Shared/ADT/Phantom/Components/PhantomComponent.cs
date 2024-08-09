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

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId PhantomHauntAction = "ActionPhantomHaunt";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? PhantomHauntActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId PhantomStopHauntAction = "ActionPhantomStopHaunt";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? PhantomStopHauntActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId PhantomMakeVesselAction = "ActionPhantomMakeVessel";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? PhantomMakeVesselActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId PhantomSelectVesselAction = "ActionPhantomCycleVessels";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? PhantomSelectVesselActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId PhantomHauntVesselAction = "ActionPhantomHauntVessel";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? PhantomHauntVesselActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId PhantomStyleAction = "ActionPhantomSelectStyle";

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public EntityUid? PhantomStyleActionEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public string CurrentStyle = "PhantomStyleMove";

    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid?> CurrentActions = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> TempContainedActions = new();
    #endregion

    #region Toggleable Actions

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsCorporeal = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ParalysisOn = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool BreakdownOn = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool StarvationOn = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool ClawsOn = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Claws = new();

    #endregion

    #region Sounds
    public SoundSpecifier HauntSound = new SoundCollectionSpecifier("PhantomHaunt")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier SpeechSound = new SoundCollectionSpecifier("PhantomSpeech")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier GhostKissSound = new SoundCollectionSpecifier("PhantomGhostKiss")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };


    public SoundSpecifier BreakdownSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/breakdown.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier BlindingSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/blinding.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier PuppeterSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/puppeter.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier InjurySound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/injury.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier NightmareSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/tyrany-nightmare.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };


    public SoundSpecifier ThirstSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/thirst.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier ParalysisSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/blinding.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier OathSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/oath.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier PsychoSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/psycho.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier TyranySound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/tyrany-nightmare.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };


    public SoundSpecifier RepairSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/repair.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier RecoverySound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/recovery.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier PortalSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/portal.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier HelpinghandSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/helping-hand-accept.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };


    public SoundSpecifier DeathmatchSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-deathmatch.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier HelpSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-help.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier OblibionSound = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/freedom-oblivion.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };


    public SoundSpecifier NightmareSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/nightmare.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier TyranySong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/tyrany.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier OblivionSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/oblivion.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier DeathmatchSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/deathmatch.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    public SoundSpecifier HelpSong = new SoundPathSpecifier("/Audio/ADT/Phantom/Music/help.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };


    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/helping-hand-accept.ogg");
    #endregion

    #region Alerts
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> VesselCountAlert = "PhantomVessels";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<AlertPrototype> EssenceCountAlert = "Essence";
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
    public string SpawnOnDeathPrototype = "Ectoplasm";

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

    [ViewVariables]
    public float Accumulator = 0;

    /// <summary>
    /// Entity that currently is haunted
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Holder = new EntityUid();

    [ViewVariables(VVAccess.ReadWrite)]
    public bool HasHaunted = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> Vessels = new List<EntityUid>();

    [ViewVariables(VVAccess.ReadWrite)]
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
    public string HallucinationsPrototype = "Changeling";

    [ViewVariables(VVAccess.ReadWrite)]
    public Container HelpingHand = default!;

    [DataField]
    public bool EpidemicActive = false;

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
    #region Finale
    [DataField]
    public bool FinalAbilityUsed = false;

    [DataField]
    public bool CanHaunt = true;

    [DataField]
    public bool NightmareStarted = false;

    [DataField]
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
