using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ItemSwitch;

/// <summary>
///     Generic enum keys for toggle-visualizer appearance data & sprite layers.
/// </summary>
[Serializable, NetSerializable]
public enum SwitchableVisuals : byte
{
    Switched,
    Layer
}
