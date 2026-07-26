using Content.Shared.MedicalScanner;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Surgery.Events;

[Serializable, NetSerializable]
public sealed class SurgeryConsoleState : BoundUserInterfaceState
{
    public bool HasTable;
    public bool HasPatient;

    public HealthAnalyzerUiState PatientHealth;

    public string GraphName;
    public string CurrentNode;

    public List<(string Label, List<string> ToolTags)> NextSteps;

    public SurgeryConsoleState(bool hasTable, bool hasPatient, HealthAnalyzerUiState patientHealth,
        string graphName, string currentNode, List<(string, List<string>)> nextSteps)
    {
        HasTable = hasTable;
        HasPatient = hasPatient;
        PatientHealth = patientHealth;
        GraphName = graphName;
        CurrentNode = currentNode;
        NextSteps = nextSteps;
    }
}
