using Content.Shared.MedicalScanner;
using Robust.Shared.GameObjects;

namespace Content.Client.PDA;

[RegisterComponent]
public sealed partial class PdaClientUiStateComponent : Component
{
    public int LastView = PdaMenu.HomeView;
    public bool MedTekSessionActive;
    public HealthAnalyzerUiState? LastHealthScan;
}
