using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ModSuits;

[Serializable, NetSerializable]
public sealed class ModLockMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public ModLockMessage(NetEntity module)
    {
        Module = module;
    }
}
