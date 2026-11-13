using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Body.Bones;

[Serializable, NetSerializable]
public sealed partial class ADTBoneReattachDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity Organ;

    private ADTBoneReattachDoAfterEvent()
    {
    }

    public ADTBoneReattachDoAfterEvent(NetEntity organ)
    {
        Organ = organ;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class ADTSplintDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity Organ;

    private ADTSplintDoAfterEvent()
    {
    }

    public ADTSplintDoAfterEvent(NetEntity organ)
    {
        Organ = organ;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class ADTSplintRemoveDoAfterEvent : DoAfterEvent
{
    [DataField]
    public NetEntity Organ;

    private ADTSplintRemoveDoAfterEvent()
    {
    }

    public ADTSplintRemoveDoAfterEvent(NetEntity organ)
    {
        Organ = organ;
    }

    public override DoAfterEvent Clone() => this;
}
