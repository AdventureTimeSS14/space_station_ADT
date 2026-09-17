using Content.Shared.ADT.LogicCircuit;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.InconnuOS;

[Serializable, NetSerializable]
public enum ADTComputerUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum OsPowerAction : byte
{
    Reboot = 0,
    Shutdown,
}

[Serializable, NetSerializable]
public sealed class OsDriveState
{
    public readonly char Letter;
    public readonly string Label;
    public readonly OsDisk Disk;
    public readonly bool Removable;
    public readonly bool ReadOnly;
    public readonly int Capacity;

    public OsDriveState(
        char letter,
        string label,
        OsDisk disk,
        bool removable,
        bool readOnly,
        int capacity)
    {
        Letter = letter;
        Label = label;
        Disk = disk;
        Removable = removable;
        ReadOnly = readOnly;
        Capacity = capacity;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsBuiState : BoundUserInterfaceState
{
    public readonly string MachineName;
    public readonly string UserName;
    public readonly OsDriveState[] Drives;
    public readonly ProtoId<ADTOsAppPrototype>[] Apps;
    public readonly OsSettings Settings;
    public readonly OsLimits Limits;
    public readonly TimeSpan BootedAt;
    public readonly bool Activated;
    public readonly bool Crashed;

    public ADTOsBuiState(
        string machineName,
        string userName,
        OsDriveState[] drives,
        ProtoId<ADTOsAppPrototype>[] apps,
        OsSettings settings,
        OsLimits limits,
        TimeSpan bootedAt,
        bool activated,
        bool crashed)
    {
        MachineName = machineName;
        UserName = userName;
        Drives = drives;
        Apps = apps;
        Settings = settings;
        Limits = limits;
        BootedAt = bootedAt;
        Activated = activated;
        Crashed = crashed;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsFileWriteMessage : BoundUserInterfaceMessage
{
    public readonly string Path;
    public readonly OsFileKind Kind;
    public readonly string Text;
    public readonly LogicCircuitLayout? Circuit;

    public ADTOsFileWriteMessage(string path, OsFileKind kind, string text, LogicCircuitLayout? circuit)
    {
        Path = path;
        Kind = kind;
        Text = text;
        Circuit = circuit;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsFileDeleteMessage : BoundUserInterfaceMessage
{
    public readonly string Path;

    public ADTOsFileDeleteMessage(string path)
    {
        Path = path;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsFileMoveMessage : BoundUserInterfaceMessage
{
    public readonly string From;
    public readonly string To;
    public readonly bool Copy;

    public ADTOsFileMoveMessage(string from, string to, bool copy)
    {
        From = from;
        To = to;
        Copy = copy;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsCreateDirectoryMessage : BoundUserInterfaceMessage
{
    public readonly string Path;

    public ADTOsCreateDirectoryMessage(string path)
    {
        Path = path;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsSettingsMessage : BoundUserInterfaceMessage
{
    public readonly OsSettings Settings;

    public ADTOsSettingsMessage(OsSettings settings)
    {
        Settings = settings;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsPowerMessage : BoundUserInterfaceMessage
{
    public readonly OsPowerAction Action;

    public ADTOsPowerMessage(OsPowerAction action)
    {
        Action = action;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsCommandMessage : BoundUserInterfaceMessage
{
    public readonly string Line;

    public ADTOsCommandMessage(string line)
    {
        Line = line;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsOutputMessage : BoundUserInterfaceMessage
{
    public readonly string[] Lines;
    public readonly string WorkingDirectory;

    public ADTOsOutputMessage(string[] lines, string workingDirectory)
    {
        Lines = lines;
        WorkingDirectory = workingDirectory;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsCircuitStateMessage : BoundUserInterfaceMessage
{
    public readonly ADTLogicCircuitBuiState State;

    public ADTOsCircuitStateMessage(ADTLogicCircuitBuiState state)
    {
        State = state;
    }
}

[Serializable, NetSerializable]
public struct OsPortInfo
{
    public string Port;
    public LogicSignal Value;
    public int Links;
}

[Serializable, NetSerializable]
public sealed class ADTOsPortsMessage : BoundUserInterfaceMessage
{
    public readonly OsPortInfo[] Inputs;
    public readonly OsPortInfo[] Outputs;

    public ADTOsPortsMessage(OsPortInfo[] inputs, OsPortInfo[] outputs)
    {
        Inputs = inputs;
        Outputs = outputs;
    }
}

[Serializable, NetSerializable]
public sealed class ADTOsRequestCircuitMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class ADTOsErrorMessage : BoundUserInterfaceMessage
{
    public readonly OsValidationError Error;
    public readonly string Detail;

    public ADTOsErrorMessage(OsValidationError error, string detail)
    {
        Error = error;
        Detail = detail;
    }
}
