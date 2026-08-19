using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.ADT.CCVar;
using Content.Shared.Body.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.NPC.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Xenobiology.Systems;

public partial class XenobiologySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly NpcFactionSystem _factions = default!;

    private void SubscribeBreeding()
    {
        SubscribeLocalEvent<SlimeComponent, MapInitEvent>(OnSlimeMapInit);
        SubscribeLocalEvent<SlimeComponent, ComponentShutdown>(OnSlimeShutdown);
        SubscribeLocalEvent<RehydratableComponent, GotRehydratedEvent>(OnSlimeRehydrated);
    }

    private void OnSlimeRehydrated(Entity<RehydratableComponent> ent, ref GotRehydratedEvent args)
    {
        if (_net.IsClient || !TryComp<SlimeComponent>(args.Target, out var slime))
            return;

        slime.MutationChance *= _random.NextFloat(0.5f, 1.5f);
        slime.MaxOffspring += _random.Next(-1, 2);
        slime.ExtractsProduced += _random.Next(0, 2);
        slime.MitosisHunger *= _random.NextFloat(0.75f, 1.2f);
        Dirty(args.Target, slime);
    }

    private void OnSlimeShutdown(Entity<SlimeComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient)
            return;

        if (ent.Comp.Stomach.ContainedEntities.Count > 0)
        {
            _containerSystem.EmptyContainer(ent.Comp.Stomach);
        }
    }

    private void OnSlimeMapInit(Entity<SlimeComponent> ent, ref MapInitEvent args)
    {
        if (!_net.IsServer) return;

        ApplyBreed(ent, ent.Comp.Breed);

        Subs.CVar(_configuration, SimpleStationCCVars.XenobiologyBreedingInterval, val => ent.Comp.UpdateInterval = TimeSpan.FromSeconds(val), true);
        ent.Comp.NextUpdateTime = _gameTiming.CurTime + ent.Comp.UpdateInterval;
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
            var hunger = _hunger.GetHunger(ent);

            if (hunger > ent.Comp1.MitosisHunger - ent.Comp1.JitterDifference)
                _jitter.DoJitter(ent, TimeSpan.FromSeconds(1), true);

            if (hunger < ent.Comp1.MitosisHunger)
                continue;

            DoMitosis(ent);
        }
    }

    private void DoMitosis(Entity<SlimeComponent> ent)
    {
        if (_net.IsClient)
            return;

        var offspringCount = _random.Next(1, ent.Comp.MaxOffspring + 1);

        var slowdown = GetBreedingSlowdown(ent);
        if (slowdown >= 1f)
            return;

        if (slowdown > 0f)
            offspringCount = Math.Max(1, (int)MathF.Round(offspringCount * (1f - slowdown)));

        _audio.PlayPredicted(ent.Comp.MitosisSound, ent, ent);

        List<EntityUid> slimes = [];

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
                newSlime.Friendship = ent.Comp.Friendship;
                newSlime.MutationChance = ent.Comp.MutationChance;
                newSlime.MaxOffspring = ent.Comp.MaxOffspring;
                newSlime.ExtractsProduced = ent.Comp.ExtractsProduced;
                slimes.Add(sl.Value.Owner);
            }
        }

        var slimeScale = 1f / (float)slimes.Count;
        var parentStomachList = new List<Entity<StomachComponent>>();
        if (TryComp<BodyComponent>(ent.Owner, out var bodyComp))
            _body.TryGetOrgansWithComponent(new Entity<BodyComponent?>(ent.Owner, bodyComp), out parentStomachList);
        var parentStomachSolutionTransfer = new Solution();
        foreach (var stomach in parentStomachList)
        {
            if (_solutionContainer.ResolveSolution(stomach.Owner, StomachSystem.DefaultSolutionName, ref stomach.Comp.Solution, out var sol))
            {
                parentStomachSolutionTransfer.AddSolution(sol, _prototypeManager);
                sol.RemoveAllSolution();
            }
        }
        parentStomachSolutionTransfer.ScaleSolution(slimeScale);

        var parentChemSolutionTransfer = new Solution();
        if (TryComp<BloodstreamComponent>(ent, out var parentBloodstream)
            && _solutionContainer.ResolveSolution(ent.Owner, parentBloodstream.BloodSolutionName, ref parentBloodstream.BloodSolution, out var parentChem))
        {
            parentChemSolutionTransfer.AddSolution(parentChem, _prototypeManager);
            parentChem.RemoveAllSolution();
        }
        parentChemSolutionTransfer.ScaleSolution(slimeScale);

        foreach (var s in slimes)
        {
            if (TryComp<BloodstreamComponent>(s, out var childBloodstream)
                && _solutionContainer.ResolveSolution(s, childBloodstream.BloodSolutionName, ref childBloodstream.BloodSolution, out var childChem))
                childChem.AddSolution(parentChemSolutionTransfer, _prototypeManager);

            var childStomachList = new List<Entity<StomachComponent>>();
            if (TryComp<BodyComponent>(s, out var childBodyComp))
                _body.TryGetOrgansWithComponent(new Entity<BodyComponent?>(s, childBodyComp), out childStomachList);
            foreach (var stomach in childStomachList)
            {
                _stomach.TryTransferSolution(stomach.Owner, parentStomachSolutionTransfer, stomach);
            }
        }

        MakeMitosisFriends(ent, slimes);

        _containerSystem.EmptyContainer(ent.Comp.Stomach);
        RaiseLocalEvent(ent, new SlimeMitosisEvent(slimes));
        QueueDel(ent);
    }

    private void MakeMitosisFriends(Entity<SlimeComponent> ent, List<EntityUid> offspring)
    {
        if (_net.IsClient)
            return;

        var range = ent.Comp.FriendSightRange;
        if (range <= 0f)
            return;

        var coords = Transform(ent).Coordinates;
        var witnessed = new HashSet<EntityUid>();

        foreach (var player in _lookup.GetEntitiesInRange<ActorComponent>(coords, range))
        {
            var playerUid = player.Owner;

            if (ent.Comp.LatchedTarget is { } latchTarget && latchTarget == playerUid)
                continue;

            witnessed.Add(playerUid);
        }

        if (witnessed.Count == 0)
            return;

        _factions.IgnoreEntities(new Entity<FactionExceptionComponent?>(ent.Owner, default), witnessed);

        foreach (var child in offspring)
        {
            _factions.IgnoreEntities(new Entity<FactionExceptionComponent?>(child, default), witnessed);
        }
    }

    private Entity<SlimeComponent>? SpawnSlime(EntityUid parent, EntProtoId newEntityProto, ProtoId<BreedPrototype> selectedBreed)
    {
        if (Deleted(parent)
        || !_prototypeManager.TryIndex(selectedBreed, out _) || _net.IsClient)
            return null;

        var newEntityUid = SpawnNextToOrDrop(newEntityProto, parent);
        if (!TryComp<SlimeComponent>(newEntityUid, out var newSlime))
            return null;

        ApplyBreed((newEntityUid, newSlime), selectedBreed);

        return new Entity<SlimeComponent>(newEntityUid, newSlime);
    }

    private void ApplyBreed(Entity<SlimeComponent> ent, ProtoId<BreedPrototype> breedId)
    {
        if (!_prototypeManager.TryIndex(breedId, out var breed))
            return;

        var slime = ent.Comp;
        slime.Breed = breedId;
        slime.SlimeColor = breed.SlimeColor;
        slime.MaxOffspring = breed.MaxOffspring;
        slime.MutationChance = breed.MutationChance;
        slime.PotentialMutations = breed.PotentialMutations;
        slime.ShouldHaveShader = breed.ShouldHaveShader;
        slime.Shader = breed.Shader;

        if (slime.ShouldHaveShader && slime.Shader != null)
            _appearance.SetData(ent, XenoSlimeVisuals.Shader, slime.Shader);

        _appearance.SetData(ent, XenoSlimeVisuals.Color, slime.SlimeColor);
        _mobGrowth.SetBaseName(ent, Loc.GetString(breed.BreedName));
        Dirty(ent);
    }

    /// <summary>
    /// Counts the number of slimes on the same grid as the given slime.
    /// </summary>
    public int GetLocalSlimeDensity(Entity<SlimeComponent> ent)
    {
        if (_net.IsClient)
            return 0;

        var gridId = Transform(ent).GridUid;
        var count = 0;

        var query = EntityQueryEnumerator<SlimeComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (uid == ent.Owner)
                continue;

            if (xform.GridUid == gridId)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Computes a breeding slowdown factor in the range [0, 1] based on local slime density.
    /// 0 means no slowdown (breeding unaffected), approaching 1 means breeding is nearly halted.
    /// The slowdown ramps up progressively between the slowdown-start threshold and the max cap.
    /// </summary>
    public float GetBreedingSlowdown(Entity<SlimeComponent> ent)
    {
        var max = _configuration.GetCVar(SimpleStationCCVars.XenobiologyMaxSlimesPerGrid);
        var start = _configuration.GetCVar(SimpleStationCCVars.XenobiologyBreedingSlowdownStart);
        var factor = _configuration.GetCVar(SimpleStationCCVars.XenobiologyBreedingSlowdownFactor);

        if (max <= 0 || factor <= 0)
            return 0f;

        var density = GetLocalSlimeDensity(ent);
        if (density <= start)
            return 0f;

        var range = Math.Max(1, max - start);
        var raw = (density - start) / (float)range;
        return Math.Clamp(raw * factor, 0f, 1f);
    }
}
