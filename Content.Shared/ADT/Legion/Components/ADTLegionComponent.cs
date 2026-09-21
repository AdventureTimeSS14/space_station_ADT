using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Legion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ADTLegionComponent : Component
{
    [DataField]
    public EntProtoId BroodProto = "ADTMobLegionSkull";

    [DataField]
    public TimeSpan SummonCooldown = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSummon;

    [DataField]
    public string BodyContainerId = "adt_legion_body";

    [DataField]
    public EntProtoId? CorpseProto = "RandomCargoCorpseSpawner";

    [DataField]
    public EntProtoId? CoreProto = "ADTLegionCore";

    [DataField]
    public LocId DeathMessage = "adt-legion-death";

    [DataField]
    public List<LocId> SpeechLines = new()
    {
        "adt-legion-speech-1",
        "adt-legion-speech-2",
        "adt-legion-speech-3",
    };

    [DataField]
    public List<LocId> EmoteLines = new()
    {
        "adt-legion-emote-1",
        "adt-legion-emote-2",
        "adt-legion-emote-3",
        "adt-legion-emote-4",
        "adt-legion-emote-5",
    };

    [DataField]
    public float SpeechChance = 0.5f;

    [DataField]
    public TimeSpan SpeechDelayMin = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan SpeechDelayMax = TimeSpan.FromSeconds(25);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSpeech;
}
