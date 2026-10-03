using Content.Server.Botany.Components;
using Content.Shared.PowerCell;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.ADT.PlantAnalyzer;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Traits.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Atmos;

namespace Content.Server.Botany.Systems; // This is how it supposed to be

public sealed class PlantAnalyzerSystem : EntitySystem
{
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private PowerCellSystem _cell = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PlantAnalyzerComponent, PlantAnalyzerSetMode>(OnModeSelected);
    }

    private void OnAfterInteract(Entity<PlantAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !_cell.HasActivatableCharge(ent.Owner, user: args.User))
            return;

        if (ent.Comp.DoAfter != null)
            return;

        if (!TryGetPlantSource(args.Target.Value, out _, out _, out _))
            return;

        var delay = ent.Comp.Settings.AdvancedScan ? ent.Comp.Settings.AdvScanDelay : ent.Comp.Settings.ScanDelay;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay, new PlantAnalyzerDoAfterEvent(), ent, target: args.Target, used: ent)
        {
            NeedHand = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.01f
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs, out ent.Comp.DoAfter);
    }

    private void OnDoAfter(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerDoAfterEvent args)
    {
        ent.Comp.DoAfter = null;
        // Double charge use for advanced scan.
        if (ent.Comp.Settings.AdvancedScan)
        {
            if (!_cell.TryUseActivatableCharge(ent.Owner, user: args.User))
                return;
        }
        if (args.Handled || args.Cancelled || args.Args.Target == null || !_cell.TryUseActivatableCharge(ent.Owner, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningEndSound, ent);

        OpenUserInterface(args.User, ent);
        UpdateScannedUser(ent, args.Args.Target.Value);

        args.Handled = true;
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

        if (!TryGetPlantSource(target, out var snapshot, out var protoId, out var isTray))
            return;

        var state = ObtainingGeneData(snapshot, protoId, target, isTray, ent.Comp.Settings.AdvancedScan);
        if (state != null)
            _uiSystem.ServerSendUiMessage(ent.Owner, PlantAnalyzerUiKey.Key, state);
    }

    private bool TryGetPlantSource(EntityUid target, out EntityUid? snapshot, out EntProtoId? protoId, out bool isTray)
    {
        snapshot = null;
        protoId = null;
        isTray = false;

        if (TryComp<SeedComponent>(target, out var seed))
        {
            snapshot = seed.PlantData;
            protoId = seed.PlantProtoId;
            return true;
        }

        isTray = true;
        if (HasComp<PlantTrayComponent>(target))
        {
            if (!_plantTray.TryGetPlant(target, out var plant))
                return false;

            snapshot = plant;
            return true;
        }

        if (!HasComp<PlantComponent>(target))
            return false;

        snapshot = target;
        return true;
    }

    private bool TryGet<T>(EntityUid? snapshot, EntProtoId? protoId, [NotNullWhen(true)] out T? comp)
        where T : class, IComponent, new()
    {
        return _botany.TryGetPlantComponent(snapshot, protoId, out comp);
    }

    public PlantAnalyzerScannedSeedPlantInformation? ObtainingGeneData(EntityUid? snapshot, EntProtoId? protoId, EntityUid target, bool isTray, bool scanIsAdvanced)
    {
        if (!TryGet<PlantComponent>(snapshot, protoId, out var plant))
            return null;

        TryGet<PlantDataComponent>(snapshot, protoId, out var data);
        TryGet<PlantChemicalsComponent>(snapshot, protoId, out var chemicals);
        TryGet<PlantConsumeExudeGasComponent>(snapshot, protoId, out var gases);

        var harvestType = AnalyzerHarvestType.Unknown;
        if (TryGet<PlantHarvestComponent>(snapshot, protoId, out var harvest))
        {
            harvestType = harvest.HarvestRepeat switch
            {
                HarvestType.Repeat => AnalyzerHarvestType.Repeat,
                HarvestType.NoRepeat => AnalyzerHarvestType.NoRepeat,
                HarvestType.SelfHarvest => AnalyzerHarvestType.SelfHarvest,
                _ => AnalyzerHarvestType.Unknown,
            };
        }

        List<string> mutationStrings = new();
        if (data != null)
        {
            foreach (var mutationProto in data.MutationPrototypes)
            {
                if (ProtoMan.TryIndex(mutationProto, out var proto)
                    && proto.TryComp<PlantDataComponent>(out var mutationData, _componentFactory))
                {
                    mutationStrings.Add(mutationData.Name);
                }
            }
        }

        PlantAnalyzerScannedSeedPlantInformation ret = new()
        {
            TargetEntity = GetNetEntity(target),
            IsTray = isTray,
            SeedName = data?.Name,
            SeedChem = chemicals?.Chemicals.Keys.Select(x => x.Id).ToArray() ?? Array.Empty<string>(),
            HarvestType = harvestType,
            ExudeGases = GetGasFlags(gases?.ExudeGasses.Keys ?? Enumerable.Empty<Gas>()),
            ConsumeGases = GetGasFlags(gases?.ConsumeGasses.Keys ?? Enumerable.Empty<Gas>()),
            Endurance = plant.Endurance,
            SeedYield = plant.Yield,
            Lifespan = plant.Lifespan,
            Maturation = plant.Maturation,
            Production = plant.Production,
            GrowthStages = plant.GrowthStages,
            SeedPotency = plant.Potency,
            Speciation = mutationStrings.ToArray()
        };

        if (scanIsAdvanced)
        {
            var growth = TryGet<PlantGrowthComponent>(snapshot, protoId, out var g) ? g : new PlantGrowthComponent();
            var atmos = TryGet<PlantAtmosphericComponent>(snapshot, protoId, out var a) ? a : new PlantAtmosphericComponent();
            var toxins = TryGet<PlantToxinsComponent>(snapshot, protoId, out var t) ? t : new PlantToxinsComponent();
            var weedPest = TryGet<PlantWeedPestComponent>(snapshot, protoId, out var w) ? w : new PlantWeedPestComponent();

            ret.AdvancedInfo = new AdvancedScanInfo
            {
                NutrientConsumption = growth.NutrientConsumption,
                WaterConsumption = growth.WaterConsumption,
                IdealHeat = (atmos.LowHeatTolerance + atmos.HighHeatTolerance) / 2f,
                HeatTolerance = (atmos.HighHeatTolerance - atmos.LowHeatTolerance) / 2f,
                ToxinsTolerance = toxins.ToxinsTolerance,
                LowPressureTolerance = atmos.LowPressureTolerance,
                HighPressureTolerance = atmos.HighPressureTolerance,
                PestTolerance = weedPest.PestTolerance,
                WeedTolerance = weedPest.WeedTolerance,
                Mutations = GetMutationFlags(snapshot, protoId),
            };
        }

        return ret;
    }

    public MutationFlags GetMutationFlags(EntityUid? snapshot, EntProtoId? protoId)
    {
        var ret = MutationFlags.None;
        if (TryGet<PlantTraitKudzuComponent>(snapshot, protoId, out _))
            ret |= MutationFlags.TurnIntoKudzu;
        if (TryGet<PlantTraitSeedlessComponent>(snapshot, protoId, out _))
            ret |= MutationFlags.Seedless;
        if (TryGet<PlantTraitLigneousComponent>(snapshot, protoId, out _))
            ret |= MutationFlags.Ligneous;
        if (TryGet<PlantTraitScreamComponent>(snapshot, protoId, out _))
            ret |= MutationFlags.CanScream;

        return ret;
    }

    public GasFlags GetGasFlags(IEnumerable<Gas> gases)
    {
        var gasFlags = GasFlags.None;
        foreach (var gas in gases)
        {
            switch (gas)
            {
                case Gas.Nitrogen:
                    gasFlags |= GasFlags.Nitrogen;
                    break;
                case Gas.Oxygen:
                    gasFlags |= GasFlags.Oxygen;
                    break;
                case Gas.CarbonDioxide:
                    gasFlags |= GasFlags.CarbonDioxide;
                    break;
                case Gas.Plasma:
                    gasFlags |= GasFlags.Plasma;
                    break;
                case Gas.Tritium:
                    gasFlags |= GasFlags.Tritium;
                    break;
                case Gas.WaterVapor:
                    gasFlags |= GasFlags.WaterVapor;
                    break;
                case Gas.Ammonia:
                    gasFlags |= GasFlags.Ammonia;
                    break;
                case Gas.NitrousOxide:
                    gasFlags |= GasFlags.NitrousOxide;
                    break;
                case Gas.Frezon:
                    gasFlags |= GasFlags.Frezon;
                    break;
            }
        }
        return gasFlags;
    }

    private void OnModeSelected(Entity<PlantAnalyzerComponent> ent, ref PlantAnalyzerSetMode args)
    {
        SetMode(ent, args.AdvancedScan);
    }

    public void SetMode(Entity<PlantAnalyzerComponent> ent, bool isAdvMode)
    {
        if (ent.Comp.DoAfter != null)
            return;
        ent.Comp.Settings.AdvancedScan = isAdvMode;
    }
}
