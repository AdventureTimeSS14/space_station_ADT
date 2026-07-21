using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Antag;

[Serializable, NetSerializable]
public sealed class RequestAntagRollBonusInfoEvent : EntityEventArgs;

[Serializable, NetSerializable]
public sealed class AntagRollBonusInfoEvent : EntityEventArgs
{
    public readonly Dictionary<ProtoId<AntagPrototype>, float> Bonuses;

    public AntagRollBonusInfoEvent(Dictionary<ProtoId<AntagPrototype>, float> bonuses)
    {
        Bonuses = bonuses;
    }
}
