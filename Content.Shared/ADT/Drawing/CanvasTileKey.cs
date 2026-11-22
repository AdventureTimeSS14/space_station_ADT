using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

[Serializable, NetSerializable]
public readonly record struct CanvasTileKey(byte Layer, ushort X, ushort Y);
