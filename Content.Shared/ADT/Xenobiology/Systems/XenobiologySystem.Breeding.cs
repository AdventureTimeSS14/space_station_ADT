using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Xenobiology.Systems;

/// <summary>
/// This handles slime breeding and mutation.
/// </summary>
public partial class XenobiologySystem
{
    private void InitializeBreeding() =>
        SubscribeLocalEvent<SlimeComponent, MapInitEvent>(OnSlimeInit);

    private void OnSlimeInit(Entity<SlimeComponent> slime, ref MapInitEvent args)
    {
        Subs.CVar(_configuration, ADTCCVars.BreedingInterval, val => slime.Comp.UpdateInterval = TimeSpan.FromSeconds(val), true);
        slime.Comp.NextUpdateTime = _gameTiming.CurTime + slime.Comp.UpdateInterval;
    }

    // Checks slime entity hunger threshholds, if the threshhold required by SlimeComponent is met -> DoMitosis.
    private void UpdateMitosis()
    {
        var eligibleSlimes = new HashSet<Entity<SlimeComponent, MobGrowthComponent, HungerComponent>>();

        var query = EntityQueryEnumerator<SlimeComponent, MobGrowthComponent, HungerComponent>();
        while (query.MoveNext(out var uid, out var slime, out var growthComp, out var hungerComp))
        {
            if (_gameTiming.CurTime < slime.NextUpdateTime
                || _mobState.IsDead(uid)
                || growthComp.IsFirstStage)
                continue;

            eligibleSlimes.Add((uid, slime, growthComp, hungerComp));
            slime.NextUpdateTime = _gameTiming.CurTime + slime.UpdateInterval;
        }

        foreach (var ent in eligibleSlimes)
        {
            if (_hunger.GetHunger(ent) > ent.Comp1.MitosisHunger - ent.Comp1.JitterDifference)
                _jitter.DoJitter(ent, TimeSpan.FromSeconds(1), true);

            if (_hunger.GetHunger(ent) < ent.Comp1.MitosisHunger)
                continue;

            DoMitosis(ent);
        }
    }

    #region Helpers

    /// <summary>
    /// Spawns a slime with a given mutation
    /// </summary>
    /// <param name="parent">The original entity.</param>
    /// <param name="newEntityProto">The proto of the entity being spawned.</param>
    /// <param name="selectedBreed">The selected breed of the entity.</param>
    public void DoBreeding(EntityUid parent, EntProtoId newEntityProto, ProtoId<BreedPrototype> selectedBreed)
    {
        if (!_prototypeManager.TryIndex(selectedBreed, out var newBreed)
            || _net.IsClient)
            return;

        var newEntityUid = SpawnNextToOrDrop(newEntityProto, parent, null, newBreed.Components);
        if (!_slimeQuery.TryComp(newEntityUid, out var slime))
            return;

        if (slime is { ShouldHaveShader: true, Shader: not null })
            _appearance.SetData(newEntityUid, XenoSlimeVisuals.Shader, slime.Shader);

        _appearance.SetData(newEntityUid, XenoSlimeVisuals.Color, slime.SlimeColor);
        _metaData.SetEntityName(newEntityUid, newBreed.BreedName);

        if (HasComp<RandomBreedOnSpawnComponent>(parent))
            QueueDel(parent);
    }

    //Handles slime mitosis, for each offspring, a mutation is selected from their potential mutations - if mutation is successful, the products of mitosis will have the new mutation.
    private void DoMitosis(Entity<SlimeComponent> ent)
    {
        if (_net.IsClient)
            return;

        var offspringCount = _random.Next(1, ent.Comp.MaxOffspring + 1);
        _audio.PlayPredicted(ent.Comp.MitosisSound, ent, ent);

        for (var i = 0; i < offspringCount; i++)
        {
            var selectedBreed = ent.Comp.Breed;

            if (_random.Prob(ent.Comp.MutationChance) && ent.Comp.PotentialMutations.Count > 0)
                selectedBreed = _random.Pick(ent.Comp.PotentialMutations);
            else if (ent.Comp.MutationChance >= 0.7f && ent.Comp.SpecialPotentialMutations.Count > 0 &&
                _random.Prob(ent.Comp.SpecialMutationChance))
                selectedBreed = _random.Pick(ent.Comp.SpecialPotentialMutations);

            DoBreeding(ent, ent.Comp.DefaultSlimeProto, selectedBreed);
        }

        _containerSystem.EmptyContainer(ent.Comp.Stomach);
        QueueDel(ent);
    }

    #endregion
}
