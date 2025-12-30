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
    private void InitializeBreeding()
    {
        SubscribeLocalEvent<PendingSlimeSpawnComponent, MapInitEvent>(OnPendingSlimeMapInit);
        SubscribeLocalEvent<SlimeComponent, MapInitEvent>(OnSlimeMapInit);
        SubscribeLocalEvent<RandomBreedOnSpawnComponent, MapInitEvent>(OnSlimeInit);
    }

    private void OnPendingSlimeMapInit(Entity<PendingSlimeSpawnComponent> ent, ref MapInitEvent args)
    {
        if (!_net.IsServer) return;

        var slime = SpawnSlime(ent, ent.Comp.BasePrototype, ent.Comp.Breed);
        QueueDel(ent);

        if (!slime.HasValue)
            return;

        var s = slime.Value.Comp;

        s.MutationChance *= _random.NextFloat(.5f, 1.5f);
        s.MaxOffspring += _random.Next(-1, 2);
        s.ExtractsProduced += _random.Next(0, 2);
        s.MitosisHunger *= _random.NextFloat(.75f, 1.2f);
    }

    private void OnSlimeInit(Entity<RandomBreedOnSpawnComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<SlimeComponent>(ent, out var slime))
            return;

        var selectedBreed = _random.Pick(ent.Comp.Mutations);
        var sl = SpawnSlime(ent, slime.DefaultSlimeProto, selectedBreed);
        QueueDel(ent);

        if (!sl.HasValue)
            return;

        var s = sl.Value.Comp;

        s.MutationChance *= _random.NextFloat(.5f, 1.5f);
        s.MaxOffspring += _random.Next(-1, 2);
        s.ExtractsProduced += _random.Next(0, 2);
        s.MitosisHunger *= _random.NextFloat(.75f, 1.2f);
    }

    private void OnSlimeMapInit(Entity<SlimeComponent> slime, ref MapInitEvent args)
    {
        if (!_net.IsServer)
            return;

        Subs.CVar(_configuration, ADTCCVars.BreedingInterval, val => slime.Comp.UpdateInterval = TimeSpan.FromSeconds(val), true);
        slime.Comp.NextUpdateTime = _gameTiming.CurTime + slime.Comp.UpdateInterval;
    }

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
    private Entity<SlimeComponent>? SpawnSlime(EntityUid parent, EntProtoId newEntityProto, ProtoId<BreedPrototype> selectedBreed)
    {
        if (Deleted(parent)
        || !_prototypeManager.TryIndex(selectedBreed, out var newBreed) || _net.IsClient)
            return null;

        var newEntityUid = SpawnNextToOrDrop(newEntityProto, parent, null, newBreed.Components);
        if (!TryComp<SlimeComponent>(newEntityUid, out var newSlime))
            return null;

        if (newSlime.ShouldHaveShader && newSlime.Shader != null)
            _appearance.SetData(newEntityUid, XenoSlimeVisuals.Shader, newSlime.Shader);

        _appearance.SetData(newEntityUid, XenoSlimeVisuals.Color, newSlime.SlimeColor);
        _metaData.SetEntityName(newEntityUid, newBreed.LocalizedName);

        return new Entity<SlimeComponent>(newEntityUid, newSlime);
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

            var sl = SpawnSlime(ent, ent.Comp.DefaultSlimeProto, selectedBreed);
            if (sl.HasValue)
            {
                var newSlime = sl.Value.Comp;
                newSlime.Tamer = ent.Comp.Tamer;
                newSlime.MutationChance = ent.Comp.MutationChance;
                newSlime.MaxOffspring = ent.Comp.MaxOffspring;
                newSlime.ExtractsProduced = ent.Comp.ExtractsProduced;
            }
        }

        _containerSystem.EmptyContainer(ent.Comp.Stomach);
        QueueDel(ent);
    }

    #endregion
}
