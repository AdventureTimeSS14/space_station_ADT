using System.Numerics;
using Content.Client.ADT.Particles;
using Content.Shared.ADT.Mining;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.ADT.Particles;
using Content.Shared.Mining.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Mining;

public sealed class ADTMineralSystem : SharedADTMineralSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ParticleSystem _particles = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<ParticleEffectPrototype> Debris = "ADTMineralDebris";
    private static readonly ProtoId<ParticleEffectPrototype> ImpactSmoke = "ADTMineralImpactSmoke";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMineralOreComponent, ComponentStartup>(OnOreStartup);
        SubscribeNetworkEvent<ADTMineralHitEvent>(OnMineralHit);
    }

    private void OnOreStartup(Entity<ADTMineralOreComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.ScanOverlay is not { } overlay || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent.Owner, sprite), MiningScannerVisualLayers.Overlay, out _, false))
            return;

        var layer = _sprite.AddLayer((ent.Owner, sprite), overlay);
        _sprite.LayerMapSet((ent.Owner, sprite), MiningScannerVisualLayers.Overlay, layer);
        _sprite.LayerSetVisible((ent.Owner, sprite), layer, false);
    }

    protected override void OnHit(Entity<ADTMineralComponent> ent, EntityUid source, EntityUid? predictedBy)
    {
        if (predictedBy == null || predictedBy != _player.LocalEntity || !_timing.IsFirstTimePredicted)
            return;

        var direction = _transform.GetWorldPosition(source) - _transform.GetWorldPosition(ent);
        SpawnHitEffects(ent, direction);
    }

    private void OnMineralHit(ADTMineralHitEvent ev)
    {
        if (!TryGetEntity(ev.Mineral, out var mineral))
            return;

        SpawnHitEffects(mineral.Value, ev.Direction);
    }

    private void SpawnHitEffects(EntityUid mineral, Vector2 direction)
    {
        var coords = _transform.GetMapCoordinates(mineral);
        var angle = new Angle(MathF.Atan2(direction.X, direction.Y));
        var overrides = new ParticleRuntimeOverrides
        {
            EmitAngle = angle,
        };

        _particles.CreateParticle(ImpactSmoke, coords, overrides: overrides);
        _particles.CreateParticle(Debris, coords, overrides: overrides);
    }
}
