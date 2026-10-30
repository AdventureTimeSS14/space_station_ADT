using System.Linq;
using Content.Server.Medical;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Surgery;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Events;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.MedicalScanner;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Surgery;

public sealed partial class SurgeryConsoleSystem : SharedSurgerySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private HealthAnalyzerSystem _healthAnalyzer = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private const float TableSearchRange = 1f;
    private const float PatientSearchRange = 0.4f;

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
            if (now < comp.NextCheck)
                continue;

            comp.NextCheck = now + CheckInterval;

            var hasPatient = TryFindPatient(uid, out var hasTable, out var patient);

            if (_ui.IsUiOpen(uid, SurgeryConsoleUiKey.Key))
                RefreshUi((uid, comp), hasTable, hasPatient ? patient : null);

            CheckPatientAlerts(uid, comp, hasPatient ? patient : null, now);
        }
    }

    private void OnBeforeOpen(EntityUid uid, SurgeryConsoleComponent comp, BeforeActivatableUIOpenEvent args)
    {
        RefreshUi((uid, comp));
    }

    private void OnToggleAlerts(EntityUid uid, SurgeryConsoleComponent comp, SurgeryConsoleToggleAlertsMessage args)
    {
        comp.AlertsEnabled = args.Enabled;
        RefreshUi((uid, comp));
    }

    private void CheckPatientAlerts(EntityUid console, SurgeryConsoleComponent comp, EntityUid? patientUid, TimeSpan now)
    {
        if (!comp.AlertsEnabled
            || patientUid is not { } patient
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

        EntityUid? nearestTable = null;
        var nearestDistance = float.MaxValue;

        foreach (var table in _lookup.GetEntitiesInRange<OperatingTableComponent>(consoleCoords, TableSearchRange))
        {
            var distance = (Transform(table).Coordinates.Position - consoleCoords.Position).Length();
            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            nearestTable = table;
        }

        if (nearestTable is not { } tableUid)
            return false;

        hasTable = true;

        if (TryComp<StrapComponent>(tableUid, out var strap))
        {
            foreach (var buckled in strap.BuckledEntities)
            {
                if (!HasComp<BodyComponent>(buckled))
                    continue;

                patient = buckled;
                return true;
            }
        }

        foreach (var candidate in _lookup.GetEntitiesInRange<BodyComponent>(Transform(tableUid).Coordinates, PatientSearchRange))
        {
            if (!_standing.IsDown(candidate.Owner))
                continue;

            patient = candidate;
            return true;
        }

        return false;
    }

    private void RefreshUi(Entity<SurgeryConsoleComponent> console)
    {
        var hasPatient = TryFindPatient(console, out var hasTable, out var patient);
        RefreshUi(console, hasTable, hasPatient ? patient : null);
    }

    private void RefreshUi(Entity<SurgeryConsoleComponent> console, bool hasTable, EntityUid? patientUid)
    {
        var alerts = console.Comp.AlertsEnabled;

        if (patientUid is not { } patient)
        {
            _ui.SetUiState(console.Owner, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(hasTable, false, alerts, new HealthAnalyzerUiState(), string.Empty, string.Empty, string.Empty, new()));
            return;
        }

        var health = _healthAnalyzer.GetHealthAnalyzerUiState(patient);

        if (!TryComp<OperatedComponent>(patient, out var operated)
            || operated.GraphId is not { } graphId
            || !PrototypeManager.TryIndex<SurgeryGraphPrototype>(graphId, out var graph))
        {
            _ui.SetUiState(console.Owner, SurgeryConsoleUiKey.Key,
                new SurgeryConsoleState(true, true, alerts, health, string.Empty, string.Empty, string.Empty, new()));
            return;
        }

        var steps = new List<string>();
        var node = GetNode(graph, operated.CurrentNode);

        if (node != null)
        {
            foreach (var edge in GetPatientEdges(patient, node))
            {
                if (edge.Steps.Count == 0 || !EdgeConditionsMet(patient, edge))
                    continue;

                steps.Add(EdgeLabel(edge));
            }
        }

        var graphLabel = LocOrId(graph.Name, graph.ID);
        var location = LocOrId(graph.Location, string.Empty);
        var nodeLabel = node == null ? operated.CurrentNode : LocOrId(node.Label, operated.CurrentNode);
        _ui.SetUiState(console.Owner, SurgeryConsoleUiKey.Key,
            new SurgeryConsoleState(true, true, alerts, health, graphLabel, location, nodeLabel, steps));
    }
}
