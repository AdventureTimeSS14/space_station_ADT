using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.Lavaland.DayNight;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTLavalandDayNightComponent : Component
{
    [DataField]
    public float NightThreshold = 1f;

    [DataField]
    public List<ProtoId<NpcFactionPrototype>> Factions = new();

    [DataField]
    public float MeleeDamageMultiplier = 1f;

    [DataField]
    public float SpeedMultiplier = 1f;

    [DataField]
    public float IncomingDamageMultiplier = 1f;

    [DataField]
    public float TendrilDelayMultiplier = 1f;

    [DataField]
    public int TendrilExtraSpawns;

    [ViewVariables]
    public bool IsNight;

    [ViewVariables]
    public bool Initialized;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;
}
