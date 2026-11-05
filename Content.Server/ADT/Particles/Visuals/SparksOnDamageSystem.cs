using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Particles.Visuals;
using Content.Shared.Damage.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Particles.Visuals;

/// <summary>
/// Rolls machine sparks on the server, plays their sound, and exposes the tile to an ignition hotspot.
/// </summary>
public sealed partial class SparksOnDamageSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _activeIgnitionSources = new();
    private readonly List<EntityUid> _expiredIgnitionSources = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SparksOnDamageComponent, DamageDealtEvent>(OnDamageDealt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // This only goes through anything in _activeIgnitionSources, dormant SparksOnDamage entities do not do anything
        _expiredIgnitionSources.Clear();
        foreach (var (uid, ignitionEnds) in _activeIgnitionSources)
        {
            if (_timing.CurTime >= ignitionEnds || !TryComp(uid, out SparksOnDamageComponent? sparks) || !TryComp(uid, out TransformComponent? xform))
            {
                _expiredIgnitionSources.Add(uid);
                continue;
            }

            ExposeHotspot((uid, xform), sparks);
        }

        foreach (var uid in _expiredIgnitionSources)
        {
            _activeIgnitionSources.Remove(uid);
        }
    }

    private void OnDamageDealt(Entity<SparksOnDamageComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive() || !_random.Prob(Math.Clamp(ent.Comp.Chance, 0f, 1f)))
            return;

        _audio.PlayPvs(ent.Comp.Sound, ent.Owner);

        var xform = Transform(ent.Owner);
        _activeIgnitionSources[ent.Owner] = _timing.CurTime + ent.Comp.IgnitionDuration;
        ExposeHotspot((ent.Owner, xform), ent.Comp);

        var coordinates = _transform.GetMapCoordinates(ent.Owner, xform);
        RaiseNetworkEvent(
            new SparksOnDamageEvent(coordinates, ent.Comp.Effect.Id, ent.Comp.LightColor, ent.Comp.LightRadius, ent.Comp.LightEnergy, ent.Comp.LightDuration),
            Filter.Pvs(coordinates));
    }

    private void ExposeHotspot(Entity<TransformComponent> ent, SparksOnDamageComponent sparks)
    {
        if (ent.Comp.GridUid is not { } grid)
            return;

        var tile = _transform.GetGridOrMapTilePosition(ent.Owner, ent.Comp);
        _atmosphere.HotspotExpose(grid, tile, sparks.IgnitionTemperature, sparks.IgnitionVolume, ent.Owner, true);
    }
}
