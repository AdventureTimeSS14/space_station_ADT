using Content.Shared.ADT.Construction.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Construction;

/// <summary>
/// Well-known machine part type ids.
/// </summary>
public static class MachinePartIds
{
    public static readonly ProtoId<MachinePartPrototype> Capacitor = "Capacitor";
    public static readonly ProtoId<MachinePartPrototype> Servo = "Servo";
    public static readonly ProtoId<MachinePartPrototype> MatterBin = "MatterBin";
    public static readonly ProtoId<MachinePartPrototype> ScanningModule = "ScanningModule";
    public static readonly ProtoId<MachinePartPrototype> MicroLaser = "MicroLaser";
    public static readonly ProtoId<MachinePartPrototype> PowerCell = "PowerCell";
}
