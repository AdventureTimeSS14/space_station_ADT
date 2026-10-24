using Content.Client.Camera;
using Content.Shared.ADT.Heretic.Components.Moon;
using Robust.Client.Player;
using Robust.Shared.Random;

namespace Content.Client.ADT.Heretic.Systems.Moon;

public sealed class MoonConfusionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly CameraRecoilSystem _recoil = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private float _accum;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalEntity;
        if (player == null || !TryComp<MoonConfusedComponent>(player, out var confused))
            return;

        _accum += frameTime;
        if (_accum < confused.ShakeInterval)
            return;

        _accum = 0f;
        var strength = confused.ShakeStrength;
        var kick = _random.NextVector2(-1f, 1f) * strength;
        _recoil.KickCamera(player.Value, kick);
    }
}