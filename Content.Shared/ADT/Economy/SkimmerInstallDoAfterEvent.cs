using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Economy;

[Serializable, NetSerializable]
public sealed partial class SkimmerInstallDoAfterEvent : SimpleDoAfterEvent
{
}
