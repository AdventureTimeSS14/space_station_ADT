using Content.Shared.ADT.Salvage;
using Content.Shared.Camera;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;

    private readonly HashSet<Entity<ActorComponent>> _screenActors = new();

    public void FlashAndShakeScreens(EntityUid source, int range, Color color, TimeSpan flashDuration, float shakeStrength)
    {
        var center = _transform.GetMapCoordinates(source);

        if (center.MapId == MapId.Nullspace)
            return;

        _screenActors.Clear();
        _lookup.GetEntitiesInRange(center, range * MathF.Sqrt(2f) + 0.5f, _screenActors);

        var ev = new ADTMegafaunaScreenFlashEvent(color, (float) flashDuration.TotalSeconds);

        foreach (var actor in _screenActors)
        {
            var coords = _transform.GetMapCoordinates(actor.Owner);

            if (ADTMegafaunaGeometry.ChebyshevDistance(center, coords) > range + 0.5f)
                continue;

            RaiseNetworkEvent(ev, actor.Comp.PlayerSession);

            if (shakeStrength <= 0f)
                continue;

            var kick = _random.NextAngle().ToVec() * shakeStrength;
            _recoil.KickCamera(actor.Owner, kick);
        }
    }
}
