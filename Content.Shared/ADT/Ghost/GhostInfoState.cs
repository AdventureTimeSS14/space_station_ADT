using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Ghost
{

    [Serializable, NetSerializable]
    public sealed class GhostInfoUpdateState : BoundUserInterfaceState
    {
        public string? StationAlertLevel;
        public Color StationAlertColor;
        public GhostInfoUpdateState(string? stationAlertLevel, Color stationAlertColor)
        {
            StationAlertLevel = stationAlertLevel;
            StationAlertColor = stationAlertColor;
        }
    }
}
