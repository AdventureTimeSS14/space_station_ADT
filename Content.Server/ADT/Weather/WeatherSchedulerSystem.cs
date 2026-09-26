using Content.Server.ADT.Lavaland.Events;
using Content.Shared.ADT.Lavaland.Events;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weather;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Weather;

public sealed class WeatherSchedulerSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedWeatherSystem _weather = default!;
    [Dependency] private readonly ADTLavalandEventSystem _lavalandEvents = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WeatherSchedulerComponent>();
        while (query.MoveNext(out var map, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            AdvanceStage(map, comp, now, false);
        }
    }

    public void SetStage(EntityUid map, WeatherSchedulerComponent comp, int stage, bool instant)
    {
        comp.Stage = stage;
        comp.NextUpdate = _timing.CurTime;

        if (instant)
            AdvanceStage(map, comp, _timing.CurTime, true);

        Dirty(map, comp);
    }

    private void RemoveOtherWeather(EntityUid map, EntProtoId? keep)
    {
        if (!_statusEffects.TryEffectsWithComp<WeatherStatusEffectComponent>(map, out var effects))
            return;

        foreach (var effect in effects)
        {
            if (Prototype(effect) is not { } proto)
                continue;

            if (keep is { } kept && proto.ID == kept.Id)
                continue;

            _statusEffects.TryRemoveStatusEffect(map, proto.ID);
        }
    }

    private void AdvanceStage(EntityUid map, WeatherSchedulerComponent comp, TimeSpan now, bool instant)
    {
        if (comp.PendingEvent is { } pending)
        {
            comp.PendingEvent = null;
            _lavalandEvents.RunEvent(map, pending);
        }

        if (comp.Stage >= comp.Stages.Count)
            comp.Stage = 0;

        var stage = comp.Stages[comp.Stage++];
        var duration = TimeSpan.FromSeconds(stage.Duration.Next(_random));
        comp.NextUpdate = now + duration;

        var (stageWeather, stageMessage, stageEvent) = PickVariant(stage); 
        comp.PendingEvent = stageEvent;

        if (instant)
            RemoveOtherWeather(map, stageWeather);

        var mapId = Comp<MapComponent>(map).MapId;
        if (stageWeather is {} weather)
        {
            if (!instant && HasWeather(comp, comp.Stage - 1))
                duration += SharedWeatherSystem.StartupTime;
            if (HasWeather(comp, comp.Stage + 1))
                duration += SharedWeatherSystem.ShutdownTime;
            _weather.TryAddWeather(map, weather, out _, duration);

            if (instant)
                _statusEffects.TrySetStatusEffectStartTime(map, weather, now - SharedWeatherSystem.StartupTime);
        }

        if (stageMessage is {} message)
        {
            var msg = Loc.GetString(message);
            _chat.ChatMessageToManyFiltered(
                Filter.BroadcastMap(mapId),
                ChatChannel.Radio,
                msg,
                msg,
                map,
                false,
                true,
                null);
        }
    }

    private (EntProtoId? Weather, LocId? Message, ProtoId<ADTLavalandEventPrototype>? Event) PickVariant(WeatherStage stage)
    {
        if (stage.Variants.Count == 0)
            return (stage.Weather, stage.Message, stage.EventOnEnd);

        var total = 0f;
        foreach (var variant in stage.Variants)
        {
            total += MathF.Max(variant.Weight, 0f);
        }

        if (total <= 0f)
            return (stage.Weather, stage.Message, stage.EventOnEnd);

        var roll = _random.NextFloat(total);
        foreach (var variant in stage.Variants)
        {
            roll -= MathF.Max(variant.Weight, 0f);
            if (roll <= 0f)
                return (variant.Weather, variant.Message, variant.EventOnEnd);
        }

        var last = stage.Variants[^1];
        return (last.Weather, last.Message, last.EventOnEnd);
    }

    private bool HasWeather(WeatherSchedulerComponent comp, int stage)
    {
        if (stage < 0)
            stage = comp.Stages.Count + stage;
        else if (stage >= comp.Stages.Count)
            stage %= comp.Stages.Count;

        var entry = comp.Stages[stage];
        return entry.Weather != null || entry.Variants.Count > 0;
    }
}