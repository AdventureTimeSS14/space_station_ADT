using Content.Shared.FixedPoint; // ADT-Tweak
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
/// On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public HealthAnalyzerUiState State;

    /// <summary>
    /// When true, the client should open/navigate to the analyzer UI (e.g. PDA MedTek).
    /// Continuous tick updates keep this false so reopening a PDA does not force MedTek.
    /// </summary>
    public bool OpenUi; // ADT-Tweak

    public HealthAnalyzerScannedUserMessage(HealthAnalyzerUiState state, bool openUi = false) // ADT-Tweak
    {
        State = state;
        OpenUi = openUi; // ADT-Tweak
    }
}

/// <summary>
/// Contains the current state of a health analyzer control. Used for the health analyzer and cryo pod.
/// </summary>
[Serializable, NetSerializable]
public struct HealthAnalyzerUiState
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool? ScanMode;
    public bool? Bleeding;
    public bool? Unrevivable;
    public List<(string ReagentId, FixedPoint2 Quantity)>? MetabolizingReagents; // ADT-Tweak - list of metabolizing reagents inside scanned user

    public HealthAnalyzerUiState(NetEntity? targetEntity, float temperature, float bloodLevel, bool? scanMode, bool? bleeding, bool? unrevivable, List<(string ReagentId, FixedPoint2 Quantity)>? metabolizingReagents = null) // Starlight - added metabolizingReagents parameter
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        MetabolizingReagents = metabolizingReagents; // ADT-Tweak
    }
}
