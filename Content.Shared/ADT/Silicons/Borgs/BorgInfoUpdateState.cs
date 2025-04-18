using Robust.Shared.Serialization;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Silicons.Borgs
{

    [Serializable, NetSerializable]
    public sealed class BorgInfoUpdateState : BoundUserInterfaceState
    {
        public BorgInfoStation StationInfo;
        public string BorgName;
        public float ChargePercent;
        public bool HasBattery;
        public BorgInfoUpdateState(string borgName, BorgInfoStation stationInfo, float chargePercent, bool hasBattery)
        {
            BorgName = borgName;
            StationInfo = stationInfo;
            ChargePercent = chargePercent;
            HasBattery = hasBattery;
        }
    }
    [Serializable, NetSerializable]
    public struct BorgInfoStation
    {
        public string? StationName;
        public string? StationAlertLevel;
        public Color StationAlertColor;
    }
}
