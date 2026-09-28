using Content.Server.Administration.Systems;
using Content.Server.Antag.Selectors;
using Content.Server.GameTicking;
using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

[RegisterComponent, Access(typeof(AntagSelectionSystem), typeof(AdminVerbSystem))]
public sealed partial class AntagSelectionComponent : Component
{
    /// <summary>
    /// Has the primary assignment of antagonists been handled yet?
    /// This is typically set to true at the start of antag assignment for a game rule.
    /// Note that this can be true even before all antags have been assigned.
    /// </summary>
    [DataField]
    public bool AssignmentHandled;

    /// <summary>
    /// Has the antagonists been preselected but yet to be fully assigned?
    /// </summary>
    [DataField]
    public bool PreSelectionsComplete;

    /// <summary>
    /// If true, players that late join into a round have a chance of being converted into antagonists for this game rule.
    /// </summary>
    [DataField]
    public bool LateJoinAdditional;

    /// <summary>
    /// The antag specifiers for the antagonists
    /// </summary>
    [DataField(required: true)]
    public AntagCountSelector[] Antags;

    /// <summary>
    /// Cached sessions of antag definitions and selected players.
    /// Players in this dict are not guaranteed to have been assigned the role yet, and may be removed if they fail to initialize as an antag.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagSpecifierPrototype>, HashSet<ICommonSession>> PreSelectedSessions = new();

    /// <summary>
    /// The minds and original names of the players assigned to be antagonists, as well as their assigned antag.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagSpecifierPrototype>, HashSet<(EntityUid uid, string name)>> AssignedMinds = new();

    /// <summary>
    /// When the antag selection will occur.
    /// </summary>
    [DataField]
    public AntagSelectionTime SelectionTime = AntagSelectionTime.RuleStarted;

    /// <summary>
    /// Locale id for the name of the antag.
    /// If this is set then the antag is listed in the round-end summary.
    /// </summary>
    [DataField]
    public LocId? AgentName;

    /// <summary>
    /// If the player is pre-selected but fails to spawn in (e.g. due to only having antag-immune jobs selected),
    /// should they be removed from the pre-selection list?
    /// </summary>
    [DataField]
    public bool RemoveUponFailedSpawn = true;
}

/// <remarks>
///     Regardless of this value, antags are only initialized after the game rule activates.
///     If a game rule does not have a delayed activation, the antag will be initialized at the same time as this enum.
///     Otherwise, it will not be initialized until the game rule becomes active.
/// </remarks>
public enum AntagSelectionTime : byte
{
    /// <summary>
    /// Antag roles are selected at <see cref="RulePlayerSpawningEvent"/>
    /// </summary>
    PrePlayerSpawn,

    /// <summary>
    /// Antag roles are selected at <see cref="RulePlayerJobsAssignedEvent"/>
    /// </summary>
    JobsAssigned,

    /// <summary>
    /// Antag roles are selected at <see cref="GameRuleStartedEvent"/>
    /// or <see cref="RulePlayerJobsAssignedEvent"/> if the game rule was started before spawning.
    /// This is the latest an antag can be selected.
    /// </summary>
    RuleStarted,

    /// <summary>
    /// Antag roles are *never* selected. Instead, this definition only makes ghost roles.
    /// </summary>
<<<<<<< HEAD
    [DataField]
    public int Min = 1;

    /// <summary>
    /// The maximum number of this antag.
    /// </summary>
    [DataField]
    public int Max = 1;

    /// <summary>
    /// A range used to randomly select <see cref="Min"/>
    /// </summary>
    [DataField]
    public MinMax? MinRange;

    /// <summary>
    /// A range used to randomly select <see cref="Max"/>
    /// </summary>
    [DataField]
    public MinMax? MaxRange;

    /// <summary>
    /// a player to antag ratio: used to determine the amount of antags that will be present.
    /// </summary>
    [DataField]
    public int PlayerRatio = 10;

    /// <summary>
    /// Whether or not players should be picked to inhabit this antag or not.
    /// If no players are left and <see cref="SpawnerPrototype"/> is set, it will make a ghost role.
    /// </summary>
    [DataField]
    public bool PickPlayer = true;

    // Corvax-start
    /// <summary>
    /// The entity that will spawn the antagonist.
    ///  Works if you select <see cref="SpawnerPrototype"/> PrePlayerSpawn
    /// If null, the player character will spawn (if you haven't added other components)
    /// </summary>
    [DataField]
    public EntProtoId? RoundstartEntity = null;
    // Corvax-end

    /// <summary>
    /// If true, players that latejoin into a round have a chance of being converted into antagonists.
    /// </summary>
    [DataField]
    public bool LateJoinAdditional = false;

    //todo: find out how to do this with minimal boilerplate: filler department, maybe?
    //public HashSet<ProtoId<JobPrototype>> JobBlacklist = new()

    /// <remarks>
    /// Mostly just here for legacy compatibility and reducing boilerplate
    /// </remarks>
    [DataField]
    public bool AllowNonHumans = false;

    /// <summary>
    /// A whitelist for selecting which players can become this antag.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist for selecting which players can become this antag.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Components added to the player.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Components added to the player's mind.
    /// Do NOT use this to add role-type components. Add those as MindRoles instead
    /// </summary>
    [DataField]
    public ComponentRegistry MindComponents = new();

    /// <summary>
    /// List of Mind Role Prototypes to be added to the player's mind.
    /// </summary>
    [DataField]
    public List<EntProtoId>? MindRoles;

    /// <summary>
    /// A set of starting gear that's equipped to the player.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    /// <summary>
    /// A list of role loadouts, from which a randomly selected one will be equipped.
    /// </summary>
    [DataField]
    public List<ProtoId<RoleLoadoutPrototype>>? RoleLoadout;

    /// <summary>
    /// A briefing shown to the player.
    /// </summary>
    [DataField]
    public BriefingData? Briefing;

    /// <summary>
    /// A spawner used to defer the selection of this particular definition.
    /// </summary>
    /// <remarks>
    /// Not the cleanest way of doing this code but it's just an odd specific behavior.
    /// Sue me.
    /// </remarks>
    [DataField]
    public EntProtoId? SpawnerPrototype;
}

/// <summary>
/// Contains data used to generate a briefing.
/// </summary>
[DataDefinition]
public partial struct BriefingData
{
    /// <summary>
    /// The text shown
    /// </summary>
    [DataField]
    public LocId? Text;

    /// <summary>
    /// The color of the text.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// The sound played.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
=======
    Never,
>>>>>>> wizards-filtered
}
