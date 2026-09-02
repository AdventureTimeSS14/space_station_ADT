using Content.Shared.ADT.Shizophrenia.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Shizophrenia;

public sealed partial class HallucinateEntityEffectSystem : EntityEffectSystem<CanHallucinateComponent, Hallucinate>
{
    [Dependency] private SchizophreniaSystem _schiz = default!;

    protected override void Effect(Entity<CanHallucinateComponent> entity, ref EntityEffectEvent<Hallucinate> args)
    {
        foreach (var item in args.Effect.HallucinationPacks)
        {
            _schiz.AddOrAdjustHallucinations(entity.Owner, item, args.Effect.Time * args.Scale, args.Effect.Type);
        }
    }
}
