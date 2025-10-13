using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ModSuits;

[Serializable, NetSerializable]
public sealed class ModModuleDeactivateMessage : BoundUserInterfaceMessage
{
    public NetEntity Module;

    public ModModuleDeactivateMessage(NetEntity module)
    {
        Module = module;
    }
}
