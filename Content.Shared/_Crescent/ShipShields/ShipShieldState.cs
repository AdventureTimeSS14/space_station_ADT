using Robust.Shared.Serialization;

namespace Content.Shared._Crescent.ShipShields;

[Serializable, NetSerializable]
public struct ShipShieldState
{
    public bool HasShield;
    public bool Online;
    public float Percent;
    public TimeSpan? RechargeEndTime;

    public ShipShieldState(bool hasShield, bool online, float percent, TimeSpan? rechargeEndTime)
    {
        HasShield = hasShield;
        Online = online;
        Percent = percent;
        RechargeEndTime = rechargeEndTime;
    }
}
