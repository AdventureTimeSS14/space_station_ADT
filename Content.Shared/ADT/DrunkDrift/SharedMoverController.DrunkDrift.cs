using System.Numerics;
using Content.Shared.ADT.DrunkDrift;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;

namespace Content.Shared.Movement.Systems;

/// <summary>ADT: пьяное шатание, детерминированное для клиента и сервера.</summary>
public abstract partial class SharedMoverController
{
    /// <summary>Частота плавного покачивания: полный цикл ~4 секунды.</summary>
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

        // Фаза и рывки от хэша сущности: клиент и сервер считают одинаково.
        var phaseRandom = new System.Random(SharedRandomExtensions.HashCodeCombine(netEnt.Id));
        var phase = phaseRandom.NextSingle() * MathF.Tau;

        var sway = drunkDrift.SwayAmplitude
                   * MathF.Sin((float)seconds * (float)DrunkSwayFrequency + phase);

        var lurchInterval = drunkDrift.LurchInterval;
        var bucket = (int)(seconds / lurchInterval);
        var bucketRandom = new System.Random(SharedRandomExtensions.HashCodeCombine(netEnt.Id, bucket));
        var lurch = 0f;
        if (bucketRandom.Prob(drunkDrift.LurchChance))
        {
            var progress = (float)(seconds - bucket * lurchInterval) / lurchInterval;
            var envelope = MathF.Sin(MathF.PI * Math.Clamp(progress, 0f, 1f));
            var direction = bucketRandom.Next(2) == 0 ? 1f : -1f;
            lurch = direction * drunkDrift.LurchAngle * envelope;
        }

        var angle = sway + lurch;
        if (angle == 0f)
            return;

        wishDir = new Angle(angle).RotateVec(wishDir);
    }
}
