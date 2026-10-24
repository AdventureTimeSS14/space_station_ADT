using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shields;

[Serializable, NetSerializable]
public enum ShieldGeneratorUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ShieldGeneratorBuiState : BoundUserInterfaceState
{
    public ShieldRunningState Running;
    public bool Overloaded;
    public float FieldIntegrity;
    public float CurrentEnergy;     // MJ
    public float MaxEnergy;         // MJ
    public float InputCap;          // W
    public float MaxInputCap;       // W
    public float CurrentUpkeep;     // W
    public int TotalSegments;
    public int FunctionalSegments;
    public float OfflineFor;
    public bool EmergencyShutdown;
    public int ConduitsDeployed;
    public int RequiredConduits;
    public bool Hacked;
    public List<ShieldGeneratorModeEntry> Modes = new();
}

[Serializable, NetSerializable]
public sealed class ShieldGeneratorModeEntry
{
    public ShieldModes Flag;
    public string Name = string.Empty;
    public string Description = string.Empty;
    public float Multiplier;
    public bool Enabled;
    public bool HackedOnly;
    public bool Hackable;
}

[Serializable, NetSerializable]
public sealed class ShieldGeneratorToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ShieldGeneratorEmergencyShutdownMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ShieldGeneratorSetInputCapMessage : BoundUserInterfaceMessage
{
    public float InputCap;
}

[Serializable, NetSerializable]
public sealed class ShieldGeneratorToggleModeMessage : BoundUserInterfaceMessage
{
    public ShieldModes Mode;
}