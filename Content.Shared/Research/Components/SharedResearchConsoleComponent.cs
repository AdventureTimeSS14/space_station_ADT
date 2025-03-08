using Content.Shared.ADT.Research;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public int Points;

        /// <summary>
        /// Доступные для изучения, изученные, либо неизвестные прототипы.
        /// ADT Research menu rework field
        /// </summary>
        public Dictionary<string, ResearchAvailablity> Researches;
        public ResearchConsoleBoundInterfaceState(int points, Dictionary<string, ResearchAvailablity> list)    // ADT Research menu rework tweaked
        {
            Points = points;
            Researches = list;   // ADT Research menu rework field
        }
    }
}
