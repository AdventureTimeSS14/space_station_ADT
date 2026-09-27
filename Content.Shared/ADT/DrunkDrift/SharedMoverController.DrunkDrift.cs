using System.Numerics;
using Content.Shared.ADT.DrunkDrift;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private const double DrunkSwayFrequency = Math.PI * 0.5;

    private EntityQuery<ADTDrunkDriftComponent> _drunkDriftQuery;

    private void InitializeDrunkDrift()
    {
        _drunkDriftQuery = GetEntityQuery<ADTDrunkDriftComponent>();
    }

    private void ApplyDrunkWobble(EntityUid uid, ref Vector2 wishDir)
    {
        if (wishDir == Vector2.Zero)
            return;

        if (!_drunkDriftQuery.TryComp(uid, out var drunkDrift) || !drunkDrift.VisualsActive)
            return;

        var netEnt = GetNetEntity(uid);
        var seconds = Timing.CurTick.Value / (double)Timing.TickRate;

        // Keep the phase and lurches deterministic without per-tick allocations.
        var phase = ToUnit(Seed(netEnt.Id)) * MathF.Tau;

        var sway = drunkDrift.SwayAmplitude
                   * MathF.Sin((float)seconds * (float)DrunkSwayFrequency + phase);

        var lurchInterval = drunkDrift.LurchInterval;
        var bucket = (int)(seconds / lurchInterval);
        var bucketSeed = Seed(netEnt.Id, bucket);
        var lurch = 0f;
        if (ToUnit(bucketSeed) < drunkDrift.LurchChance)
        {
            var progress = (float)(seconds - bucket * lurchInterval) / lurchInterval;
            var envelope = MathF.Sin(MathF.PI * Math.Clamp(progress, 0f, 1f));
            var direction = (Mix(unchecked(bucketSeed + 1u)) & 1u) == 0 ? 1f : -1f;
            lurch = direction * drunkDrift.LurchAngle * envelope;
        }

        var angle = sway + lurch;
        if (angle == 0f)
            return;

        wishDir = new Angle(angle).RotateVec(wishDir);
    }

    private static uint Seed(int entityId, int bucket = 0)
    {
        unchecked
        {
            var hash = 5381u;
            hash = ((hash << 5) + hash) + (uint) entityId;
            hash = ((hash << 5) + hash) + (uint) bucket;
            return hash;
        }
    }

    private static float ToUnit(uint value)
    {
        return (Mix(value) >> 8) * (1f / 16777216f);
    }

    private static uint Mix(uint value)
    {
        value ^= value >> 16;
        value *= 0x7feb352du;
        value ^= value >> 15;
        value *= 0x846ca68bu;
        return value ^ (value >> 16);
    }
}
