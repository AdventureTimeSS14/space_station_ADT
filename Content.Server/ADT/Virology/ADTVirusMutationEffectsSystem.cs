using Content.Shared.ADT.Virology.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Virology;

public sealed class ADTEvolveVirusEffectSystem : EntityEffectSystem<SolutionComponent, ADTEvolveVirus>
{
    [Dependency] private ADTDiseaseSystem _disease = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<ADTEvolveVirus> args)
    {
        var effect = args.Effect;
        var count = Math.Clamp((int) args.Scale, 1, ADTDiseaseSystem.SymptomLimit);
        _disease.MutateSolutionAdvance(entity, disease =>
        {
            for (var i = 0; i < count; i++)
            {
                _disease.Evolve(disease, effect.LevelMin, effect.LevelMax);
            }
        });
    }
}

public sealed class ADTDevolveVirusEffectSystem : EntityEffectSystem<SolutionComponent, ADTDevolveVirus>
{
    [Dependency] private ADTDiseaseSystem _disease = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<ADTDevolveVirus> args)
    {
        var count = Math.Clamp((int) args.Scale, 1, ADTDiseaseSystem.SymptomLimit);
        _disease.MutateSolutionAdvance(entity, disease =>
        {
            for (var i = 0; i < count; i++)
            {
                _disease.Devolve(disease);
            }
        });
    }
}

public sealed class ADTMixVirusesEffectSystem : EntityEffectSystem<SolutionComponent, ADTMixViruses>
{
    [Dependency] private ADTDiseaseSystem _disease = default!;

    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<ADTMixViruses> args)
    {
        _disease.MergeSolutionViruses(entity);
    }
}
