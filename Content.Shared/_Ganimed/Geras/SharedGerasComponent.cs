using Robust.Shared.Serialization;

namespace Content.Shared._Ganimed.Geras;

[Access(typeof(SharedGerasSystem))]
public abstract partial class SharedGerasComponent : Component
{
}

[Serializable, NetSerializable]
public enum GeraColor
{
    Color,
}