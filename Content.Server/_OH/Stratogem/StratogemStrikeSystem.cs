using System.Numerics;
using Content.Shared._OH.Stratogem;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Explosion.EntitySystems;

namespace Content.Server._OH.Stratogem;

/// <summary>
/// Handles ticking active stratogem strike zones: periodically calling in explosions
/// within their radius, and denying strikes that land within range of a <see cref="StratogemJammerComponent"/>.
/// </summary>
public sealed partial class StratogemStrikeSystem : EntitySystem
{
    [Dependency] private ExplosionSystem _explosion = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StratogemStrikeComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<StratogemStrikeComponent> ent, ref MapInitEvent args)
    {
        var coords = _transform.GetMapCoordinates(ent.Owner);

        if (ent.Comp.CheckJammers && IsJammed(coords))
        {
            // Prevent Update() from still firing an impact during the same tick the deferred deletion is processed in.
            ent.Comp.NextImpact = TimeSpan.MaxValue;
            _popup.PopupCoordinates(Loc.GetString("stratogem-strike-jammed"), Transform(ent.Owner).Coordinates);
            QueueDel(ent.Owner);
            return;
        }

        ent.Comp.NextImpact = _timing.CurTime;
    }

    private bool IsJammed(MapCoordinates coords)
    {
        var query = EntityQueryEnumerator<StratogemJammerComponent, TransformComponent>();
        while (query.MoveNext(out var jammerUid, out var jammer, out var jammerXform))
        {
            if (!jammer.Enabled)
                continue;

            var jammerCoords = _transform.GetMapCoordinates(jammerUid, jammerXform);
            if (jammerCoords.MapId != coords.MapId)
                continue;

            if ((jammerCoords.Position - coords.Position).Length() <= jammer.Range)
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<StratogemStrikeComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextImpact > curTime)
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            var coords = _transform.GetMapCoordinates(uid);
            var angle = _random.NextFloat(0f, MathF.PI * 2f);
            var distance = MathF.Sqrt(_random.NextFloat()) * comp.Radius;
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            var impactCoords = new MapCoordinates(coords.Position + offset, coords.MapId);

            _explosion.QueueExplosion(impactCoords,
                comp.ExplosionType,
                comp.TotalIntensity,
                comp.IntensitySlope,
                comp.MaxIntensity,
                null);

            if (comp.ImpactSound != null)
                _audio.PlayPvs(comp.ImpactSound, uid);

            comp.ImpactsDone++;
            comp.NextImpact = curTime + comp.Interval;

            if (comp.ImpactsDone >= comp.ImpactCount)
                QueueDel(uid);
        }
    }
}
