using Robust.Shared.Network;
using Robust.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Shared.ADT.Minesweeper
{
    [Serializable, NetSerializable]
    public sealed class MinesweeperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public List<MinesweeperRecord> Records { get; }

        public MinesweeperBoundUserInterfaceState(List<MinesweeperRecord> records)
        {
            Records = records;
        }
    }
}
