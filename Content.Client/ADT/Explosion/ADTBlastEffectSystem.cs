using Content.Client.ADT.Particles;
using Content.Shared.ADT.Explosion;
using Content.Shared.ADT.Particles;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.ADT.Explosion;

public sealed class ADTBlastEffectSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ADTScreenShakeSystem _shake = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId ExplosionEffect = "ADTEffectBlast";
    private static readonly EntProtoId ExplosionLight = "ADTEffectBlastLight";

    private static readonly ProtoId<ParticleEffectPrototype> SmokeWave = "ADTBlastSmokeWave";
    private static readonly ProtoId<ParticleEffectPrototype> SmokeWaveSmall = "ADTBlastSmokeWaveSmall";
    private static readonly ProtoId<ParticleEffectPrototype> Smoke = "ADTBlastSmoke";
    private static readonly ProtoId<ParticleEffectPrototype> SmokeSmall = "ADTBlastSmokeSmall";
    private static readonly ProtoId<ParticleEffectPrototype> SmokeDevastation = "ADTBlastSmokeDevastation";
    private static readonly ProtoId<ParticleEffectPrototype> Sparks = "ADTBlastSparks";

    private const float PixelsPerTick = 20f / 32f;
    private const int WorldView = 7;
    private const float NearShakeCap = 5f;
    private const float FarShakeCap = 1.5f;
    private const float NearShakeDuration = 15f;
    private const float FarShakeDuration = 10f;

    private ADTShockwaveOverlay _shockwaves = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ADTBlastEffectEvent>(OnExplosion);

        _shockwaves = new ADTShockwaveOverlay();
        _overlay.AddOverlay(_shockwaves);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_shockwaves);
    }

    private void OnExplosion(ADTBlastEffectEvent ev)
    {
        var explosion = ev.Explosion;
        var coords = ev.Coordinates;
        var maxRange = explosion.MaxRange;
        if (maxRange <= 0)
            return;

        CreateEffect(coords, explosion, maxRange);
        ShakeLocal(coords, explosion, maxRange);
    }

    private void CreateEffect(MapCoordinates coords, ADTBlastRadii explosion, int maxRange)
    {
        var large = explosion.Devastation > 0;
        var small = !large && explosion.Heavy <= 0;

        var effect = Spawn(ExplosionEffect, coords);
        _transform.SetWorldRotation(effect, _random.NextAngle());

        var light = Spawn(ExplosionLight, coords);
        _light.SetRadius(light, maxRange);

        float waveSpeed;
        if (large)
            waveSpeed = 6f;
        else if (small)
            waveSpeed = 3f;
        else
            waveSpeed = 5f;

        Emit(small ? SmokeWaveSmall : SmokeWave, coords, waveSpeed * maxRange * PixelsPerTick);

        ProtoId<ParticleEffectPrototype> smoke;
        if (large)
            smoke = SmokeDevastation;
        else if (small)
            smoke = SmokeSmall;
        else
            smoke = Smoke;

        Emit(smoke, coords, null);
        Emit(Sparks, coords, 8f * maxRange * PixelsPerTick);

        if (maxRange >= 6 || explosion.Heavy > 0)
            _shockwaves.Add(coords, maxRange * 0.75f, TimeSpan.FromSeconds(Math.Max(0.05f * maxRange, 0.2f)));
    }

    private void Emit(ProtoId<ParticleEffectPrototype> effect, MapCoordinates coords, float? speed)
    {
        ParticleRuntimeOverrides? overrides = null;
        if (speed != null)
        {
            overrides = new ParticleRuntimeOverrides
            {
                Speed = speed,
                SpeedVariance = speed * 0.1f,
            };
        }

        _particles.CreateParticle(effect, coords, overrides: overrides);
    }

    private void ShakeLocal(MapCoordinates coords, ADTBlastRadii explosion, int maxRange)
    {
        if (_player.LocalEntity is not { } local)
            return;

        var listener = _transform.GetMapCoordinates(local);
        if (listener.MapId != coords.MapId)
            return;

        var delta = listener.Position - coords.Position;
        var distance = (int) MathF.Max(MathF.Abs(MathF.Round(delta.X)), MathF.Abs(MathF.Round(delta.Y)));
        var baseShake = MathF.Sqrt(maxRange / (distance + 1f));
        var farDistance = explosion.Heavy * 15 + explosion.Devastation * 20;

        if (distance <= maxRange + WorldView - 2)
        {
            _shake.Shake(local, NearShakeDuration, Math.Clamp(baseShake, 0f, NearShakeCap));
            return;
        }

        if (distance <= farDistance)
        {
            if (baseShake <= 0f && explosion.Devastation <= 0)
                return;

            baseShake = MathF.Max(baseShake, explosion.Devastation * 3f);
            _shake.Shake(local, FarShakeDuration, MathF.Min(baseShake, FarShakeCap));
            return;
        }

        if (explosion.Heavy > 0 && explosion.Devastation > 0)
            _shake.Shake(local, 10f, Math.Clamp(explosion.Devastation * 0.25f, 0f, FarShakeCap));
    }
}
