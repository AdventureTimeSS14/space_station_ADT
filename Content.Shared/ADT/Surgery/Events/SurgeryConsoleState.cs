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

    public List<(string Label, List<string> ToolTags)> NextSteps;

    public SurgeryConsoleState(bool hasTable, bool hasPatient, bool alertsEnabled, HealthAnalyzerUiState patientHealth,
        string graphName, string location, string currentNode, List<(string, List<string>)> nextSteps)
    {
        HasTable = hasTable;
        HasPatient = hasPatient;
        AlertsEnabled = alertsEnabled;
        PatientHealth = patientHealth;
        GraphName = graphName;
        Location = location;
        CurrentNode = currentNode;
        NextSteps = nextSteps;
    }
}

[Serializable, NetSerializable]
public sealed class SurgeryConsoleToggleAlertsMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public SurgeryConsoleToggleAlertsMessage(bool enabled)
    {
        Enabled = enabled;
    }
}
