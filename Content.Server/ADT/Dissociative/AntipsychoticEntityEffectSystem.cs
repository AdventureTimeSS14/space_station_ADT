using Content.Server.ADT.Dissociative.Components;
using Content.Shared.ADT.Hallucinations.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Dissociative;

public sealed partial class AntipsychoticEntityEffectSystem : EntityEffectSystem<DissociativeIdentityComponent, Antipsychotic>
{
    [Dependency] private DissociativeIdentitySystem _dissociative = default!;

    protected override void Effect(Entity<DissociativeIdentityComponent> entity, ref EntityEffectEvent<Antipsychotic> args)
    {
        _dissociative.OnAntipsychotic(entity);
    }
}
