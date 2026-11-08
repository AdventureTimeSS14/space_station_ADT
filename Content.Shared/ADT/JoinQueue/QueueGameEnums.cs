using Robust.Shared.Serialization;

namespace Content.Shared.ADT.JoinQueue;

[Serializable, NetSerializable]
public enum QueueGameAction : byte
{
    FindOpponent,
    CancelSearch,
    MakeMove,
    SpinSlot,
}

[Serializable, NetSerializable]
public enum QueueGameMark : byte
{
    None,
    Cross,
    Nought,
}

[Serializable, NetSerializable]
public enum QueueGameResult : byte
{
    None,
    Win,
    Lose,
    Draw,
    OpponentLeft,
}
