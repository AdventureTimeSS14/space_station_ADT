using Content.Shared.ADT.Virology;

namespace Content.Server.ADT.Virology;

[ByRefEvent]
public record struct ADTDiseaseStageIncreaseAttemptEvent(ADTDisease Disease)
{
    public bool Cancelled;

    public bool Forced;
}

[ByRefEvent]
public record struct ADTDiseaseHasCureEvent(ADTDisease Disease)
{
    public bool Handled;

    public bool HasCure;
}

[ByRefEvent]
public record struct ADTDiseaseContractAttemptEvent(ADTDisease Disease)
{
    public bool Cancelled;
}

[ByRefEvent]
public record struct ADTDiseaseCuredEvent(ADTDisease Disease);

[ByRefEvent]
public record struct ADTDiseasesChangedEvent;
