using System.Linq;
using Content.Server.ADT.Botany.Components;
using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Server.PowerCell;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.ADT.PlantAnalyzer;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Botany.Systems;

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, BoundUIClosedEvent>(OnUIClosed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PlantAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ScannedEntity == null)
                continue;

            if (comp.NextUpdate > _timing.CurTime)
                continue;

            comp.NextUpdate = _timing.CurTime + comp.UpdateInterval;
            UpdateScannedUser((uid, comp), comp.ScannedEntity.Value);
        }
    }

    private void OnAfterInteract(Entity<PlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == null || !args.CanReach || !_cell.HasActivatableCharge(ent, user: args.User))
            return;

        args.Handled = true;

        if (!HasComp<SeedComponent>(args.Target) &&
            !(TryComp<PlantHolderComponent>(args.Target, out var plantHolder) && plantHolder.Seed != null))
            return;

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            args.User,
            ent.Comp.ScanDelay,
            new PlantAnalyzerDoAfterEvent(),
            ent,
            target: args.Target,
            used: ent)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.01f,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        if (!_cell.TryUseActivatableCharge(ent, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningEndSound, ent);

        ent.Comp.ScannedEntity = args.Args.Target.Value;
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.UpdateInterval;

        OpenUserInterface(args.User, ent);
        UpdateScannedUser(ent, args.Args.Target.Value);

        args.Handled = true;
    }

    private void OnUIClosed(Entity<PlantAnalyzerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(PlantAnalyzerUiKey.Key))
            return;

        ent.Comp.ScannedEntity = null;
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!TryComp<ActorComponent>(user, out var actor) || !_uiSystem.HasUi(analyzer, PlantAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, PlantAnalyzerUiKey.Key, actor.PlayerSession);
    }

    public void UpdateScannedUser(Entity<PlantAnalyzerComponent> ent, EntityUid target)
    {
        if (!_uiSystem.HasUi(ent, PlantAnalyzerUiKey.Key))
            return;

        PlantAnalyzerScannedSeedPlantInformation? state = null;

        if (TryComp<SeedComponent>(target, out var seedComp))
        {
            if (seedComp.Seed != null)
                state = ObtainingGeneDataSeed(seedComp.Seed, target, false, false);
            else if (seedComp.SeedId != null && _prototypeManager.TryIndex(seedComp.SeedId, out SeedPrototype? protoSeed))
                state = ObtainingGeneDataSeed(protoSeed, target, false, false);
        }
        else if (TryComp<PlantHolderComponent>(target, out var plantComp) && plantComp.Seed != null)
        {
            state = ObtainingGeneDataSeed(plantComp.Seed, target, true, plantComp.MutationLevel > 0);
        }

        if (state != null)
            _uiSystem.ServerSendUiMessage(ent.Owner, PlantAnalyzerUiKey.Key, state);
    }

    public PlantAnalyzerScannedSeedPlantInformation ObtainingGeneDataSeed(
        SeedData seedData,
        EntityUid target,
        bool isTray,
        bool isMutating)
    {
        var harvestType = seedData.HarvestRepeat switch
        {
            HarvestType.Repeat => AnalyzerHarvestType.Repeat,
            HarvestType.NoRepeat => AnalyzerHarvestType.NoRepeat,
            HarvestType.SelfHarvest => AnalyzerHarvestType.SelfHarvest,
            _ => AnalyzerHarvestType.Unknown,
        };

        var mutationStrings = new List<string>();
        foreach (var mutationProto in seedData.MutationPrototypes)
        {
            if (_prototypeManager.TryIndex(mutationProto, out var seed))
                mutationStrings.Add(seed.DisplayName);
        }

        var advancedInfo = new AdvancedScanInfo
        {
            NutrientConsumption = seedData.NutrientConsumption,
            WaterConsumption = seedData.WaterConsumption,
            IdealHeat = seedData.IdealHeat,
            HeatTolerance = seedData.HeatTolerance,
            IdealLight = seedData.IdealLight,
            LightTolerance = seedData.LightTolerance,
            ToxinsTolerance = seedData.ToxinsTolerance,
            LowPressureTolerance = seedData.LowPressureTolerance,
            HighPressureTolerance = seedData.HighPressureTolerance,
            PestTolerance = seedData.PestTolerance,
            WeedTolerance = seedData.WeedTolerance,
            Mutations = GetMutationFlags(seedData),
            Viable = seedData.Viable,
        };

        return new PlantAnalyzerScannedSeedPlantInformation
        {
            TargetEntity = GetNetEntity(target),
            IsTray = isTray,
            IsMutating = isMutating,
            SeedName = seedData.DisplayName,
            SeedChem = seedData.Chemicals.Keys.ToArray(),
            HarvestType = harvestType,
            ExudeGases = seedData.ExudeGasses.Keys.ToArray(),
            ConsumeGases = seedData.ConsumeGasses.Keys.ToArray(),
            Endurance = seedData.Endurance,
            SeedYield = seedData.Yield,
            Lifespan = seedData.Lifespan,
            Maturation = seedData.Maturation,
            Production = seedData.Production,
            GrowthStages = seedData.GrowthStages,
            SeedPotency = seedData.Potency,
            Speciation = mutationStrings.ToArray(),
            AdvancedInfo = advancedInfo,
        };
    }

    public MutationFlags GetMutationFlags(SeedData plant)
    {
        var ret = MutationFlags.None;

        if (plant.TurnIntoKudzu)
            ret |= MutationFlags.TurnIntoKudzu;

        if (plant.Seedless)
            ret |= MutationFlags.Seedless;

        if (plant.Ligneous)
            ret |= MutationFlags.Ligneous;

        if (plant.CanScream)
            ret |= MutationFlags.CanScream;

        return ret;
    }

}
