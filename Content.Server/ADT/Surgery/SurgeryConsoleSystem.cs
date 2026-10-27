using System.Linq;
using Content.Server.Medical;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Systems;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Surgery;

public sealed class SurgeryConsoleSystem : SharedSurgerySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly HealthAnalyzerSystem _healthAnalyzer = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpen);
        SubscribeLocalEvent<SurgeryConsoleComponent, SurgeryConsoleToggleAlertsMessage>(OnToggleAlerts);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SurgeryConsoleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, SurgeryConsoleUiKey.Key))
                RefreshUi(uid);

            if (now < comp.NextCheck)
                continue;

            comp.NextCheck = now + CheckInterval;
            CheckPatientAlerts(uid, comp, now);
        }
    }

    private void OnBeforeOpen(EntityUid uid, SurgeryConsoleComponent comp, BeforeActivatableUIOpenEvent args)
    {
        RefreshUi(uid);
    }

    private void OnToggleAlerts(EntityUid uid, SurgeryConsoleComponent comp, SurgeryConsoleToggleAlertsMessage args)
    {
        comp.AlertsEnabled = args.Enabled;
        RefreshUi(uid);
    }

    private void CheckPatientAlerts(EntityUid console, SurgeryConsoleComponent comp, TimeSpan now)
    {
        if (!comp.AlertsEnabled
            || !TryFindPatient(console, out _, out var patient)
            || !TryComp<OperatedComponent>(patient, out var operated)
            || operated.GraphId == null)
        {
            comp.NextAlert.Clear();
            return;
        }

        var active = new HashSet<SurgeryConsoleAlert>();

        if (_mobState.IsDead(patient))
        {
            active.Add(SurgeryConsoleAlert.Dead);
        }
        else
        {
            if (_mobState.IsCritical(patient))
                active.Add(SurgeryConsoleAlert.Critical);

            var health = _healthAnalyzer.GetHealthAnalyzerUiState(patient);

            if (!float.IsNaN(health.BloodLevel) && health.BloodLevel < comp.LowBloodThreshold)
                active.Add(SurgeryConsoleAlert.LowBlood);

            if (health.Bleeding == true)
                active.Add(SurgeryConsoleAlert.Bleeding);
        }

        foreach (var alert in comp.NextAlert.Keys.ToList())
        {
            if (!active.Contains(alert))
                comp.NextAlert.Remove(alert);
        }

        if (active.Count == 0)
            return;

        var name = Name(patient);

        foreach (var alert in active)
        {
            if (comp.NextAlert.TryGetValue(alert, out var next) && now < next)
                continue;

            comp.NextAlert[alert] = now + comp.AlertCooldown;

            var message = Loc.GetString($"surgery-console-alert-{alert.ToString().ToLowerInvariant()}", ("patient", name));
            _chat.TrySendInGameICMessage(console, message, InGameICChatType.Speak, hideChat: false, checkRadioPrefix: false, ignoreActionBlocker: true);
        }
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
        var alerts = TryComp<SurgeryConsoleComponent>(console, out var consoleComp) && consoleComp.AlertsEnabled;

        var hasPatient = TryFindPatient(console, out var hasTable, out var patient);

        if (!hasTable)
        {
            _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(false, false, alerts, new HealthAnalyzerUiState(), string.Empty, string.Empty, string.Empty, new()));
            return;
        }

        if (!hasPatient)
        {
            _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(true, false, alerts, new HealthAnalyzerUiState(), string.Empty, string.Empty, string.Empty, new()));
            return;
        }

        var health = _healthAnalyzer.GetHealthAnalyzerUiState(patient);

        if (!TryComp<OperatedComponent>(patient, out var operated)
            || operated.GraphId is not { } graphId
            || !PrototypeManager.TryIndex<SurgeryGraphPrototype>(graphId, out var graph)
            || string.IsNullOrEmpty(operated.CurrentNode))
        {
            _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(true, true, alerts, health, string.Empty, string.Empty, string.Empty, new()));
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
                var firstStep = edge.Steps[0];
                var tags = firstStep.Quality is { } quality
                    ? new List<string> { quality.Id }
                    : firstStep.Tools.Select(t => t.Id).ToList();

                steps.Add((label, tags));
            }
        }

        var graphLabel = string.IsNullOrEmpty(graph.Name) ? graph.ID : Loc.GetString(graph.Name);
        var location = string.IsNullOrEmpty(graph.Location) ? string.Empty : Loc.GetString(graph.Location);
        var nodeLabel = node == null || string.IsNullOrEmpty(node.Label) ? operated.CurrentNode : Loc.GetString(node.Label);
        _ui.SetUiState(console, SurgeryConsoleUiKey.Key,
            new SurgeryConsoleState(true, true, alerts, health, graphLabel, location, nodeLabel, steps));
    }
}
