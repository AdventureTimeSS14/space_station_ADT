using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Silicons.StationAi
{

    [Serializable, NetSerializable]
    public sealed class StationAiUpdateState : BoundUserInterfaceState
    {
        //public string AIName;
        public string? StationName;
        public string? StationAlertLevel;
        public Color StationAlertColor;
        public StationAiUpdateState(//string aIName,
         string? stationName, string? stationAlertLevel, Color stationAlertColor)
        {
            //AIName = aIName;
            StationName = stationName;
            StationAlertLevel = stationAlertLevel;
            StationAlertColor = stationAlertColor;
        }
    }
}
