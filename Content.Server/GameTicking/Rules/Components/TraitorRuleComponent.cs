using Content.Server.Codewords;
using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(TraitorRuleSystem))]
public sealed partial class TraitorRuleComponent : Component
{
    public readonly List<EntityUid> TraitorMinds = new();

    [DataField]
    public ProtoId<AntagPrototype> TraitorPrototypeId = "Traitor";

    [DataField]
    public ProtoId<CodewordFactionPrototype> CodewordFactionPrototypeId = "Traitor";

    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> ObjectiveIssuers = "TraitorCorporations";

    /// <summary>
    /// Give this traitor an Uplink on spawn.
    /// </summary>
    [DataField]
    public bool GiveUplink = true;
    //ADT-tweak-start
    /// <summary>
    /// Шанс на бразерс вместо трейтора
    /// </summary>
    // ADT-Tweak-Start
    /// <summary>
    /// Вероятность того, что предатель станет лидером кровных братьев вместо получения аплинка.
    /// </summary>
    [DataField]
    public float BloodBrotherChance = 0f;
    public ProtoId<NpcFactionPrototype> BloodBrotherFaction = "BloodBrotherFaction"; //ADT-tweak
    public ProtoId<NpcFactionPrototype> BloodBrotherLeaderFaction = "BloodBrotherLeaderFaction"; //ADT-tweak
    // ADT-Tweak-End

    /// <summary>
    /// Give this traitor the codewords.
    /// </summary>
    [DataField]
    public bool GiveCodewords = true;

    /// <summary>
    /// Give this traitor a briefing in chat.
    /// </summary>
    [DataField]
    public bool GiveBriefing = true;

    public int TotalTraitors => TraitorMinds.Count;

    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }

    /// <summary>
    /// Current state of the rule
    /// </summary>
    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;

    /// <summary>
    /// When should traitors be selected and the announcement made
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan? AnnounceAt;

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundCollectionSpecifier("ADTTraitorStart"); // ADT-Tweak: изменена коллекция звуков приветствия

    /// <summary>
    /// The amount of TC traitors start with.
    /// </summary>
    [DataField]
    public FixedPoint2 StartingBalance = 20;
}
