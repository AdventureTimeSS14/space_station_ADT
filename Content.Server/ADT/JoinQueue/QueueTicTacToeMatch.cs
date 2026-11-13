using System.Threading;
using Content.Shared.ADT.JoinQueue;
using Robust.Shared.Player;

namespace Content.Server.ADT.JoinQueue;

public sealed class QueueTicTacToeMatch
{
    private static readonly int[][] Lines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6],
    ];

    public readonly ICommonSession Cross;
    public readonly ICommonSession Nought;
    public readonly QueueGameMark[] Board = new QueueGameMark[MsgQueueGameState.BoardSize];

    public QueueGameMark Turn = QueueGameMark.Cross;

    public CancellationTokenSource? TurnTimer;

    public QueueTicTacToeMatch(ICommonSession cross, ICommonSession nought)
    {
        Cross = cross;
        Nought = nought;
    }

    public ICommonSession CurrentPlayer => Turn == QueueGameMark.Cross ? Cross : Nought;

    public QueueGameMark GetMark(ICommonSession session)
    {
        return session == Cross ? QueueGameMark.Cross : QueueGameMark.Nought;
    }

    public ICommonSession GetOpponent(ICommonSession session)
    {
        return session == Cross ? Nought : Cross;
    }

    public bool TryMove(ICommonSession session, int cell)
    {
        if (CurrentPlayer != session || cell < 0 || cell >= Board.Length || Board[cell] != QueueGameMark.None)
            return false;

        Board[cell] = Turn;
        return true;
    }

    public void NextTurn()
    {
        Turn = Turn == QueueGameMark.Cross ? QueueGameMark.Nought : QueueGameMark.Cross;
    }

    public bool HasWinner()
    {
        foreach (var line in Lines)
        {
            var mark = Board[line[0]];
            if (mark != QueueGameMark.None && mark == Board[line[1]] && mark == Board[line[2]])
                return true;
        }

        return false;
    }

    public bool IsBoardFull()
    {
        return Array.TrueForAll(Board, mark => mark != QueueGameMark.None);
    }
}
