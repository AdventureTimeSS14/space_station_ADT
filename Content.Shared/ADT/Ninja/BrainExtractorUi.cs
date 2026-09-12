using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Ninja;

[Serializable, NetSerializable]
public enum BrainExtractorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum BrainExtractorUiButton : byte
{
    StartScan,
    Eject
}

[Serializable, NetSerializable]
public sealed class BrainExtractorUiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly BrainExtractorUiButton Button;

    public BrainExtractorUiButtonPressedMessage(BrainExtractorUiButton button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public sealed class BrainExtractorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly string? OccupantName;
    public readonly bool PodConnected;
    public readonly bool PodInRange;
    public readonly bool PodOccupied;
    public readonly bool IsScanning;
    public readonly float ScanProgress;
    public readonly bool CanStartScan;
    public readonly string StatusText;

    public BrainExtractorBoundUserInterfaceState(string? occupantName, bool podConnected, bool podInRange, bool podOccupied, bool isScanning, float scanProgress, bool canStartScan, string statusText)
    {
        OccupantName = occupantName;
        PodConnected = podConnected;
        PodInRange = podInRange;
        PodOccupied = podOccupied;
        IsScanning = isScanning;
        ScanProgress = scanProgress;
        CanStartScan = canStartScan;
        StatusText = statusText;
    }
}
