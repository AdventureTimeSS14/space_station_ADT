using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.JoinQueue;

public sealed class MsgQueueGameState : NetMessage
{
    public const int BoardSize = 9;

    public override MsgGroups MsgGroup => MsgGroups.Command;

    public int Score;
    public int ScoreToSpin;
    public bool Searching;
    public bool Spinning;

    public bool InMatch;
    public string OpponentName = string.Empty;
    public QueueGameMark Mark;
    public bool YourTurn;
    public QueueGameResult LastResult;

    public QueueGameMark[] Board = new QueueGameMark[BoardSize];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Score = buffer.ReadInt32();
        ScoreToSpin = buffer.ReadInt32();
        Searching = buffer.ReadBoolean();
        Spinning = buffer.ReadBoolean();
        InMatch = buffer.ReadBoolean();
        OpponentName = buffer.ReadString();
        Mark = (QueueGameMark) buffer.ReadByte();
        YourTurn = buffer.ReadBoolean();
        LastResult = (QueueGameResult) buffer.ReadByte();

        for (var i = 0; i < BoardSize; i++)
        {
            Board[i] = (QueueGameMark) buffer.ReadByte();
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Score);
        buffer.Write(ScoreToSpin);
        buffer.Write(Searching);
        buffer.Write(Spinning);
        buffer.Write(InMatch);
        buffer.Write(OpponentName);
        buffer.Write((byte) Mark);
        buffer.Write(YourTurn);
        buffer.Write((byte) LastResult);

        foreach (var mark in Board)
        {
            buffer.Write((byte) mark);
        }
    }
}
