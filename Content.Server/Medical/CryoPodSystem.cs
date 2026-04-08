using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Medical.Components;
using Content.Server.NodeContainer.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.Atmos;
<<<<<<< HEAD
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.MedicalScanner;
using Content.Shared.Temperature.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
=======
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Medical.Cryogenics;
>>>>>>> upstreamwiz/master

namespace Content.Server.Medical;

public sealed partial class CryoPodSystem : SharedCryoPodSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
<<<<<<< HEAD
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
=======
    [Dependency] private readonly GasAnalyzerSystem _gasAnalyzerSystem = default!;
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzerSystem = default!;
    [Dependency] private readonly NodeContainerSystem _nodeContainer = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

>>>>>>> upstreamwiz/master

    public override void Initialize()
    {
        base.Initialize();

<<<<<<< HEAD
        SubscribeLocalEvent<CryoPodComponent, AfterActivatableUIOpenEvent>(OnActivateUI);
        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
        SubscribeLocalEvent<CryoPodComponent, EntRemovedFromContainerMessage>(OnEjected);
    }

    private void OnActivateUI(Entity<CryoPodComponent> entity, ref AfterActivatableUIOpenEvent args)
    {
        if (!entity.Comp.BodyContainer.ContainedEntity.HasValue)
            return;

        TryComp<TemperatureComponent>(entity.Comp.BodyContainer.ContainedEntity, out var temp);
        TryComp<BloodstreamComponent>(entity.Comp.BodyContainer.ContainedEntity, out var bloodstream);

        if (TryComp<HealthAnalyzerComponent>(entity, out var healthAnalyzer))
        {
            healthAnalyzer.ScannedEntity = entity.Comp.BodyContainer.ContainedEntity;
        }

        // TODO: This should be a state my dude
        _uiSystem.ServerSendUiMessage(
=======
        SubscribeLocalEvent<CryoPodComponent, AtmosDeviceUpdateEvent>(OnCryoPodUpdateAtmosphere);
        SubscribeLocalEvent<CryoPodComponent, GasAnalyzerScanEvent>(OnGasAnalyzed);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveCryoPodComponent, CryoPodComponent>();

        while (query.MoveNext(out var uid, out _, out var cryoPod))
        {
            if (Timing.CurTime < cryoPod.NextUiUpdateTime)
                continue;

            cryoPod.NextUiUpdateTime += cryoPod.UiUpdateInterval;
            Dirty(uid, cryoPod);
            UpdateUi((uid, cryoPod));
        }
    }

    protected override void UpdateUi(Entity<CryoPodComponent> entity)
    {
        if (!UI.IsUiOpen(entity.Owner, CryoPodUiKey.Key)
            || !TryComp(entity, out CryoPodAirComponent? air))
            return;

        var patient = entity.Comp.BodyContainer.ContainedEntity;
        var gasMix = _gasAnalyzerSystem.GenerateGasMixEntry("Cryo pod", air.Air);
        var (beakerCapacity, beaker) = GetBeakerInfo(entity);
        var injecting = GetInjectingReagents(entity);
        var health = _healthAnalyzerSystem.GetHealthAnalyzerUiState(patient);
        health.ScanMode = true;
        var hasDamage = patient is null ? false : _damageable.GetTotalDamage(patient.Value) > 0;

        UI.ServerSendUiMessage(
>>>>>>> upstreamwiz/master
            entity.Owner,
            CryoPodUiKey.Key,
            new CryoPodUserMessage(gasMix, health, beakerCapacity, beaker, injecting, hasDamage)
        );
    }

    private void OnCryoPodUpdateAtmosphere(Entity<CryoPodComponent> entity, ref AtmosDeviceUpdateEvent args)
    {
        if (!_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PortablePipeNode? portNode))
            return;

        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        _atmosphereSystem.React(cryoPodAir.Air, portNode);

        if (portNode.NodeGroup is PipeNet { NodeCount: > 1 } net)
        {
            _gasCanisterSystem.MixContainerWithPipeNet(cryoPodAir.Air, net.Air);
        }
    }

    private void OnGasAnalyzed(Entity<CryoPodComponent> entity, ref GasAnalyzerScanEvent args)
    {
        if (!TryComp(entity, out CryoPodAirComponent? cryoPodAir))
            return;

        args.GasMixtures ??= new List<(string, GasMixture?)>();
        args.GasMixtures.Add((Name(entity.Owner), cryoPodAir.Air));
        // If it's connected to a port, include the port side
        // multiply by volume fraction to make sure to send only the gas inside the analyzed pipe element, not the whole pipe system
        if (_nodeContainer.TryGetNode(entity.Owner, entity.Comp.PortName, out PipeNode? port) && port.Air.Volume != 0f)
        {
            var portAirLocal = port.Air.Clone();
            portAirLocal.Multiply(port.Volume / port.Air.Volume);
            portAirLocal.Volume = port.Volume;
            args.GasMixtures.Add((entity.Comp.PortName, portAirLocal));
        }
    }
<<<<<<< HEAD

    private void OnEjected(Entity<CryoPodComponent> cryoPod, ref EntRemovedFromContainerMessage args)
    {
        if (TryComp<HealthAnalyzerComponent>(cryoPod.Owner, out var healthAnalyzer))
        {
            healthAnalyzer.ScannedEntity = null;
        }

        // if body is ejected - no need to display health-analyzer
        _uiSystem.CloseUi(cryoPod.Owner, HealthAnalyzerUiKey.Key);
    }
}
=======
}
>>>>>>> upstreamwiz/master
