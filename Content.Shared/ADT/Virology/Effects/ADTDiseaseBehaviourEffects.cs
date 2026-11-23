using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology.Effects;

[ImplicitDataDefinitionForInheritors]
public sealed partial class ADTDiseaseSpeak : EntityEffect
{
    [DataField(required: true)]
    public List<LocId> Messages = new();

    [DataField]
    public bool Whisper;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new ADTDiseaseSpeakEvent(Messages, Whisper);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }
}

public sealed partial class ADTDiseaseSpeakEvent : EntityEffectBase<ADTDiseaseSpeakEvent>
{
    public List<LocId> Messages;

    public bool Whisper;

    public ADTDiseaseSpeakEvent(List<LocId> messages, bool whisper)
    {
        Messages = messages;
        Whisper = whisper;
    }
}

[ImplicitDataDefinitionForInheritors]
public sealed partial class ADTDiseaseMagnetPull : EntityEffect
{
    [DataField]
    public float Range = 2f;

    [DataField]
    public int Steps = 1;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new ADTDiseaseMagnetPullEvent(Range, Steps);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }
}

public sealed partial class ADTDiseaseMagnetPullEvent : EntityEffectBase<ADTDiseaseMagnetPullEvent>
{
    public float Range;

    public int Steps;

    public ADTDiseaseMagnetPullEvent(float range, int steps)
    {
        Range = range;
        Steps = steps;
    }
}
