using Content.Shared.ADT.Virology.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ADTSymptomEffect
{
    public abstract void Raise(IEntityManager entMan, EntityUid mob, ADTDisease disease, ADTSymptomState state, ADTSymptomPhase phase);
}

public abstract partial class ADTSymptomEffect<T> : ADTSymptomEffect where T : ADTSymptomEffect<T>
{
    public override void Raise(IEntityManager entMan, EntityUid mob, ADTDisease disease, ADTSymptomState state, ADTSymptomPhase phase)
    {
        if (this is not T effect)
            return;

        var ev = new ADTSymptomEvent<T>(effect, disease, state, phase);
        entMan.EventBus.RaiseLocalEvent(mob, ref ev);
    }
}

[ByRefEvent]
public record struct ADTSymptomEvent<T>(T Effect, ADTDisease Disease, ADTSymptomState State, ADTSymptomPhase Phase)
    where T : ADTSymptomEffect<T>;

public sealed class ADTSymptomState
{
    public ProtoId<ADTSymptomPrototype> Id;

    public bool Done;

    public int Counter;

    public string? Text;

    public ADTSymptomState(ProtoId<ADTSymptomPrototype> id)
    {
        Id = id;
    }
}
