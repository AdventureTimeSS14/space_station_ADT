using Content.Shared.ADT.Shizophrenia.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Shizophrenia;

public sealed partial class ReduceHallucinationsEntityEffectSystem : EntityEffectSystem<CanHallucinateComponent, ReduceHallucinations>
{
    [Dependency] private SchizophreniaSystem _schiz = default!;

    protected override void Effect(Entity<CanHallucinateComponent> entity, ref EntityEffectEvent<ReduceHallucinations> args)
    {
        _schiz.AdjustAllHallucinations(entity.Owner, -args.Effect.Time * args.Scale);
    }
}
