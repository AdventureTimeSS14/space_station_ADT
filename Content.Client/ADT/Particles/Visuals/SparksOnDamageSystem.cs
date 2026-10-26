using Content.Client.ADT.Particles;
using Content.Client.Light.Components;
using Content.Shared.ADT.Particles;
using Content.Shared.ADT.Particles.Visuals;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;

namespace Content.Client.ADT.Particles.Visuals;

/// <summary>
/// Renders machine spark bursts authorized by the server.
/// </summary>
public sealed partial class SparksOnDamageSystem : EntitySystem
{
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SparksOnDamageEvent>(OnSparks);
    }

    private void OnSparks(SparksOnDamageEvent args)
    {
        if (!_mapSystem.MapExists(args.Coordinates.MapId))
            return;

        _particles.SpawnEffect(new ProtoId<ParticleEffectPrototype>(args.Effect), args.Coordinates);

        // Short-lived flash light. Client-side only: NetSyncEnabled = false keeps it out of
        // the PVS sync so the server never learns about it.
        var lightEntity = Spawn(null, args.Coordinates);
        var light = _lights.EnsureLight(lightEntity);
        light.NetSyncEnabled = false;
        _lights.SetCastShadows(lightEntity, false, light);
        _lights.SetColor(lightEntity, args.LightColor, light);
        _lights.SetRadius(lightEntity, args.LightRadius, light);
        _lights.SetEnergy(lightEntity, args.LightEnergy, light);

        AddComp(lightEntity, new TimedDespawnComponent
        {
            Lifetime = args.LightDuration,
        });
        AddComp(lightEntity, new LightFadeComponent
        {
            Duration = args.LightDuration,
        });
    }
}
