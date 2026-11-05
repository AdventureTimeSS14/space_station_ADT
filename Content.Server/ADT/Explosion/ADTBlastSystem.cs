using Content.Server.ADT.Mining;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.ADT.Explosion;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Explosion;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Explosion;

public sealed class ADTBlastSystem : EntitySystem
{
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private ADTMineralSystem _mineral = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public static readonly ProtoId<ExplosionPrototype> ExplosionType = ADTMineralSystem.BlastExplosion;

    private const float LightTileIntensity = 2f;
    private const float HeavyTileIntensity = 6f;

    private readonly HashSet<Entity<ADTMineralComponent>> _minerals = new();

    public void Explode(MapCoordinates coords, ADTBlastRadii explosion, EntityUid? cause)
    {
        var maxRange = explosion.MaxRange;
        if (maxRange <= 0 || coords.MapId == MapId.Nullspace)
            return;

        ActMinerals(coords, explosion);
        QueueDamage(coords, explosion, cause);

        RaiseNetworkEvent(new ADTBlastEffectEvent(coords, explosion), Filter.BroadcastMap(coords.MapId));
    }

    private void ActMinerals(MapCoordinates coords, ADTBlastRadii explosion)
    {
        _minerals.Clear();
        _lookup.GetEntitiesInRange(coords, explosion.MaxRange + 0.5f, _minerals, LookupFlags.Static);

        foreach (var mineral in _minerals)
        {
            if (TerminatingOrDeleted(mineral))
                continue;

            var distance = (_transform.GetWorldPosition(mineral) - coords.Position).Length();

            int severity;
            if (distance < explosion.Devastation)
                severity = 3;
            else if (distance < explosion.Heavy)
                severity = 2;
            else if (distance < explosion.Light)
                severity = 1;
            else
                continue;

            _mineral.ExplosionAct(mineral, severity);
        }
    }

    private void QueueDamage(MapCoordinates coords, ADTBlastRadii explosion, EntityUid? cause)
    {
        var light = Math.Max(explosion.Light, Math.Max(explosion.Heavy, explosion.Devastation));
        if (light <= 0)
            return;

        float slope;
        if (explosion.Heavy > 0 && light > explosion.Heavy)
            slope = (HeavyTileIntensity - LightTileIntensity) / (light - explosion.Heavy);
        else
            slope = HeavyTileIntensity / light;

        var maxIntensity = LightTileIntensity + slope * light;
        var total = _explosion.RadiusToIntensity(light, slope, maxIntensity);

        _explosion.QueueExplosion(coords, ExplosionType, total, slope, maxIntensity, cause, canCreateVacuum: false);
    }
}
