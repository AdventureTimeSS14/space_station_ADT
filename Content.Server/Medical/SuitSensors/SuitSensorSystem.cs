using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Medical.SuitSensors;
using Robust.Shared.Timing;

namespace Content.Server.Medical.SuitSensors;

public sealed class SuitSensorSystem : SharedSuitSensorSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var sensors = EntityQueryEnumerator<SuitSensorComponent, DeviceNetworkComponent>();

        while (sensors.MoveNext(out var uid, out var sensor, out var device))
        {
            if (device.TransmitFrequency is null)
                continue;

            if (curTime < sensor.NextUpdate)
                continue;

            if (!CheckSensorAssignedStation((uid, sensor)))
                continue;

            sensor.NextUpdate += sensor.UpdateRate;

            var status = GetSensorState((uid, sensor));
            if (status == null)
                continue;

            var payload = SuitSensorToPacket(status);
            _deviceNetworkSystem.QueuePacket(uid, null, payload, device: device);
        }
    }
}
