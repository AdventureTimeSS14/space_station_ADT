using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SlotMachine;

[Serializable, NetSerializable]
public sealed partial class SlotMachineDoAfterEvent : SimpleDoAfterEvent;