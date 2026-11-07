using Content.Shared.Actions;
using Content.Shared.Heretic;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Moon;

public sealed partial class EventHereticMindGate : EntityTargetActionEvent
{
    [DataField]
    public float SelfCellular = 20f;

    [DataField]
    public float TargetCellular = 30f;
}

public sealed partial class EventHereticLunarParade : InstantWorldTargetActionEvent
{
    [DataField]
    public EntProtoId Projectile = "ProjectileLunarParade";

    [DataField]
    public float ProjectileSpeed = 4f;
}

public sealed partial class EventHereticRingleadersRise : InstantActionEvent
{
    [DataField]
    public float Radius = 5f;

    [DataField]
    public EntProtoId Illusion = "MobMoonIllusion";
}

public sealed partial class EventHereticMoonlightEcho : InstantActionEvent;

public sealed partial class EventHereticTouchOfMadness : EntityTargetActionEvent;

public sealed partial class EventHereticMoonKnowledge : HereticKnowledgeEvent;

public sealed partial class HereticAscensionMoonEvent : HereticKnowledgeEvent;
