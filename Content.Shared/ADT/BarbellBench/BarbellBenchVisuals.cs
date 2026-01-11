using Robust.Shared.Serialization;

namespace Content.Shared.ADT.BarbellBench;

[Serializable, NetSerializable]
public enum BarbellBenchState
{
    Idle,
    PerformingRep
}

[Serializable, NetSerializable]
public enum BarbellBenchVisuals : byte
{
    State,
    HasBarbell
}

