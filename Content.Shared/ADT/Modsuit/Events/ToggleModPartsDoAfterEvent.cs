using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ModSuits;

[Serializable, NetSerializable]
public sealed partial class ToggleModPartsDoAfterEvent : SimpleDoAfterEvent;
