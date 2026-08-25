// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Atmos;

[Serializable, NetSerializable]
public sealed partial class CraftMolotovDoAfterEvent : SimpleDoAfterEvent;
