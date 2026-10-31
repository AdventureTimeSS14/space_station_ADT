using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.MedicalScanner;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Events;

[Serializable, NetSerializable]
public sealed class SurgeryConsoleState : BoundUserInterfaceState
{
    public bool HasTable;
    public bool HasPatient;
    public bool AlertsEnabled;

    public HealthAnalyzerUiState PatientHealth;

    public string GraphName;
    public string Location;
    public string CurrentNode;

    public List<string> NextSteps;

    public List<SurgeryConsoleOrgan> Organs;

    public bool HeartStopped;

    public SurgeryConsoleState(bool hasTable, bool hasPatient, bool alertsEnabled, HealthAnalyzerUiState patientHealth,
        string graphName, string location, string currentNode, List<string> nextSteps,
        List<SurgeryConsoleOrgan>? organs = null, bool heartStopped = false)
    {
        HasTable = hasTable;
        HasPatient = hasPatient;
        AlertsEnabled = alertsEnabled;
        PatientHealth = patientHealth;
        GraphName = graphName;
        Location = location;
        CurrentNode = currentNode;
        NextSteps = nextSteps;
        Organs = organs ?? new();
        HeartStopped = heartStopped;
    }
}

[Serializable, NetSerializable]
public sealed record SurgeryConsoleOrgan(string Category, float Damage, OrganStage Stage, bool Robotic, bool Missing);

[Serializable, NetSerializable]
public sealed class SurgeryConsoleToggleAlertsMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public SurgeryConsoleToggleAlertsMessage(bool enabled)
    {
        Enabled = enabled;
    }
}
