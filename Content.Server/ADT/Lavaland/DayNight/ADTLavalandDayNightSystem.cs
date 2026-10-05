using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland.DayNight;

public sealed class ADTLavalandDayNightSystem : EntitySystem
{
    private static readonly TimeSpan TransitionMargin = TimeSpan.FromSeconds(1);

    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private readonly List<EntityUid> _toCalm = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NpcFactionMemberComponent, MapInitEvent>(OnFactionMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTLavalandDayNightComponent, LightCycleComponent>();
        while (query.MoveNext(out var map, out var dayNight, out var cycle))
        {
            if (dayNight.Initialized && now < dayNight.NextUpdate)
                continue;

            if (MetaData(map).EntityLifeStage < EntityLifeStage.MapInitialized)
                continue;

            var time = GetCycleTime(map, cycle);
            var night = SharedLightCycleSystem.CalculateLightLevel(cycle, time) < dayNight.NightThreshold;

            if (!dayNight.Initialized || night != dayNight.IsNight)
                SetNight(map, dayNight, night);

            dayNight.Initialized = true;
            dayNight.NextUpdate = now + GetTimeUntilTransition(cycle, dayNight, time) + TransitionMargin;
        }
    }

    public bool TryGetNight(EntityUid? map, [NotNullWhen(true)] out ADTLavalandDayNightComponent? dayNight)
    {
        if (TryComp(map, out dayNight) && dayNight.IsNight)
            return true;

        dayNight = null;
        return false;
    }

    private void OnFactionMapInit(Entity<NpcFactionMemberComponent> ent, ref MapInitEvent args)
    {
        if (TryGetNight(Transform(ent).MapUid, out var dayNight))
            TryEmpower(ent, dayNight);
    }

    private void SetNight(EntityUid map, ADTLavalandDayNightComponent dayNight, bool night)
    {
        dayNight.IsNight = night;

        if (night)
        {
            var query = EntityQueryEnumerator<NpcFactionMemberComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var faction, out var xform))
            {
                if (xform.MapUid == map)
                    TryEmpower((uid, faction), dayNight);
            }

            return;
        }

        _toCalm.Clear();
        var empowered = EntityQueryEnumerator<ADTNightEmpoweredComponent, TransformComponent>();
        while (empowered.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == map)
                _toCalm.Add(uid);
        }

        foreach (var uid in _toCalm)
        {
            RemComp<ADTNightEmpoweredComponent>(uid);
        }
    }

    private void TryEmpower(Entity<NpcFactionMemberComponent> ent, ADTLavalandDayNightComponent dayNight)
    {
        if (HasComp<MegafaunaComponent>(ent))
            return;

        if (!_faction.IsMemberOfAny((ent, ent.Comp), dayNight.Factions))
            return;

        var empowered = EnsureComp<ADTNightEmpoweredComponent>(ent);
        empowered.MeleeDamageMultiplier = dayNight.MeleeDamageMultiplier;
        empowered.SpeedMultiplier = dayNight.SpeedMultiplier;
        empowered.IncomingDamageMultiplier = dayNight.IncomingDamageMultiplier;
        Dirty(ent, empowered);
    }

    private float GetCycleTime(EntityUid map, LightCycleComponent cycle)
    {
        return (float) _timing.CurTime
            .Add(cycle.Offset)
            .Subtract(_ticker.RoundStartTimeSpan)
            .Subtract(_metaData.GetPauseTime(map))
            .TotalSeconds;
    }

    private static TimeSpan GetTimeUntilTransition(LightCycleComponent cycle, ADTLavalandDayNightComponent dayNight, float time)
    {
        var length = MathF.Max(1f, (float) cycle.Duration.TotalSeconds);
        var min = MathF.Max(0f, cycle.MinLightLevel);
        var max = MathF.Max(0f, cycle.MaxLightLevel);

        if (max <= min || dayNight.NightThreshold <= min || dayNight.NightThreshold >= MathF.Min(max, cycle.ClipLight))
            return cycle.Duration;

        var s0 = MathF.Pow((dayNight.NightThreshold - min) / (max - min), 1f / 6f);
        var a = MathF.Asin(s0) / MathF.PI;

        var u = time / length;
        u -= MathF.Floor(u);

        float target;
        if (u < a)
            target = a;
        else if (u < 1f - a)
            target = 1f - a;
        else
            target = 1f + a;

        return TimeSpan.FromSeconds((target - u) * length);
    }
}
