//

using Content.Shared.Heretic.Prototypes;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Heretic.Components;

[Serializable, NetSerializable]
public sealed class HereticRitualMessage(ProtoId<HereticRitualPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<HereticRitualPrototype> ProtoId = protoId;
}

[Serializable, NetSerializable]
public enum HereticRitualRuneUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class HereticShapeshiftMessage(ProtoId<PolymorphPrototype> protoId) : BoundUserInterfaceMessage
{
    public ProtoId<PolymorphPrototype> ProtoId = protoId;
}

[Serializable, NetSerializable]
public enum HereticShapeshiftUiKey : byte
{
    Key
}
