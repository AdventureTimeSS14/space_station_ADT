using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.GripModule;

[Serializable, NetSerializable]
public sealed partial class ADTGripModuleRemoveDoAfterEvent : SimpleDoAfterEvent
{
}
