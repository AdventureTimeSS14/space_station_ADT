using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.UI;

[Serializable, NetSerializable]
public struct ADTEntityPickerEntry
{
    public NetEntity Entity;
    public string Name;
    public EntProtoId? Proto;

    public ADTEntityPickerEntry(NetEntity entity, string name, EntProtoId? proto)
    {
        Entity = entity;
        Name = name;
        Proto = proto;
    }
}
