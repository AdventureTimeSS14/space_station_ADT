using Content.Server.ADT.Hallucinations.Components;
using Content.Shared.ADT.Hallucinations.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Hallucinations.Systems;

public sealed partial class ReduceHallucinationsEntityEffectSystem : EntityEffectSystem<CanHallucinateComponent, ReduceHallucinations>
{
    [Dependency] private SchizophreniaSystem _schiz = default!;

    protected override void Effect(Entity<CanHallucinateComponent> entity, ref EntityEffectEvent<ReduceHallucinations> args)
    {
        _schiz.AdjustAllHallucinations(entity.Owner, -args.Effect.Time * args.Scale);
    }
}
