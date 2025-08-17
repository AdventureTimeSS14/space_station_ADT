using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Ghost
{

    [Serializable, NetSerializable]
    public sealed class GhostInfoUpdateState : BoundUserInterfaceState
    {
        public string? StationName;
        public string? StationAlertLevel;
        public Color StationAlertColor;
        public GhostInfoUpdateState(string? stationName, string? stationAlertLevel, Color stationAlertColor)
        {
            StationName = stationName;
            StationAlertLevel = stationAlertLevel;
            StationAlertColor = stationAlertColor;
        }
    }
}
