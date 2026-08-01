using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Radio;

[Serializable, NetSerializable]
[ByRefEvent]
public readonly record struct RadioJobIconUpdatedEvent(string JobIconId, string JobName);
