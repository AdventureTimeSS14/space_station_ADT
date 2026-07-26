using System.Linq;
using Content.Server.Medical;
using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Body;
using Content.Shared.MedicalScanner;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.Surgery;

public sealed class SurgeryConsoleSystem : SharedSurgerySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpen);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SurgeryConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_ui.IsUiOpen(uid, SurgeryConsoleUiKey.Key))
                RefreshUi(uid);
        }
    }

    private void OnBeforeOpen(EntityUid uid, SurgeryConsoleComponent comp, BeforeActivatableUIOpenEvent args)
    {
        RefreshUi(uid);
    }

    private bool TryFindPatient(EntityUid console, out bool hasTable, out EntityUid patient)
    {
        patient = default;
        hasTable = false;

        var consoleCoords = Transform(console).Coordinates;

        var nearestTable = EntityUid.Invalid;
        var nearestDistance = float.MaxValue;

        foreach (var table in _lookup.GetEntitiesInRange<OperatingTableComponent>(consoleCoords, 1f))
        {
            var distance = (Transform(table).Coordinates.Position - consoleCoords.Position).Length();
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestTable = table;
        }

        if (nearestTable == EntityUid.Invalid)
            return false;

        hasTable = true;

        var tableCoords = Transform(nearestTable).Coordinates;
        foreach (var candidate in _lookup.GetEntitiesInRange<OperatedComponent>(tableCoords, 1f))
        {
            patient = candidate;
            return true;
        }

        foreach (var candidate in _lookup.GetEntitiesInRange<BodyComponent>(tableCoords, 1f))
        {
            patient = candidate;
            return true;
        }

        return false;
    }

    private bool EdgeConditionsMet(EntityUid patient, SurgeryGraphEdge edge)
    {
        foreach (var condition in edge.Conditions)
        {
            if (!condition.CheckWithInvert(patient, EntityManager))
                return false;
        }

        return true;
    }

    private void RefreshUi(EntityUid console)
    {
        var hasPatient = TryFindPatient(console, out var hasTable, out var patient);

        if (!hasTable)
        {
            _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(false, false, new HealthAnalyzerUiState(), string.Empty, string.Empty, new()));
            return;
        }

        if (!hasPatient)
        {
            _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(true, false, new HealthAnalyzerUiState(), string.Empty, string.Empty, new()));
            return;
        }

        var health = _healthAnalyzer.GetHealthAnalyzerUiState(patient);

        if (!TryComp<OperatedComponent>(patient, out var operated)
            || operated.GraphId is not { } graphId
            || !PrototypeManager.TryIndex<SurgeryGraphPrototype>(graphId, out var graph)
            || string.IsNullOrEmpty(operated.CurrentNode))
        {
            _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(true, true, health, string.Empty, string.Empty, new()));
            return;
        }

        var steps = new List<(string, List<string>)>();
        var node = GetNode(graph, operated.CurrentNode);

        if (node != null)
        {
            foreach (var edge in GetAllEdges(node))
            {
                if (edge.Steps.Count == 0 || !EdgeConditionsMet(patient, edge))
                    continue;

                var label = string.IsNullOrEmpty(edge.Label) ? edge.Id : Loc.GetString(edge.Label);
                var tags = edge.Steps[0].Tools.Select(t => t.Id).ToList();

                steps.Add((label, tags));
            }
        }

        var graphLabel = string.IsNullOrEmpty(graph.Name) ? graph.ID : Loc.GetString(graph.Name);
        var nodeLabel = node == null || string.IsNullOrEmpty(node.Label) ? operated.CurrentNode : Loc.GetString(node.Label);
        _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
            new SurgeryConsoleState(true, true, health, graphLabel, nodeLabel, steps));
    }
}
