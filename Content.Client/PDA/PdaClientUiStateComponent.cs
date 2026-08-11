using Content.Shared.MedicalScanner;
using Robust.Shared.GameObjects;

namespace Content.Client.PDA;

/// <summary>
/// Client-local PDA screen state. It belongs to the PDA entity rather than a
/// transient BoundUserInterface instance, so closing/reopening the BUI cannot
/// reset the selected application to Home.
/// </summary>
[RegisterComponent]
public sealed partial class PdaClientUiStateComponent : Component
{
    public int LastView = PdaMenu.HomeView;
    public bool MedTekSessionActive;
    public HealthAnalyzerUiState? LastHealthScan;
}
