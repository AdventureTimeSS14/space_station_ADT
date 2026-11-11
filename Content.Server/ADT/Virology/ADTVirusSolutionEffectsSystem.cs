using Content.Shared.ADT.Virology.Effects;
using Content.Shared.Chemistry.Components;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Virology;

public sealed class ADTVirusSolutionEffectsSystem : EntitySystem
{
    [Dependency] private ADTDiseaseSystem _disease = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionComponent, EntityEffectEvent<ADTEvolveVirus>>(OnEvolve);
        SubscribeLocalEvent<SolutionComponent, EntityEffectEvent<ADTDevolveVirus>>(OnDevolve);
    }

    private void OnEvolve(Entity<SolutionComponent> ent, ref EntityEffectEvent<ADTEvolveVirus> args)
    {
        var effect = args.Effect;
        _disease.ModifySolutionAdvance(ent, disease => _disease.Evolve(disease, effect.LevelMin, effect.LevelMax));
    }

    private void OnDevolve(Entity<SolutionComponent> ent, ref EntityEffectEvent<ADTDevolveVirus> args)
    {
        _disease.ModifySolutionAdvance(ent, disease => _disease.Devolve(disease));
    }
}
