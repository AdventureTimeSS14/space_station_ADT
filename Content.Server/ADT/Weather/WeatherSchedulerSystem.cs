using Content.Server.Chat.Managers;
using Content.Shared.Chat;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<WeatherSchedulerComponent>();
        while (query.MoveNext(out var map, out var comp))
        {
            if (now < comp.NextUpdate)
                continue;

            if (comp.Stage >= comp.Stages.Count)
                comp.Stage = 0;

            var stage = comp.Stages[comp.Stage++];
            var duration = TimeSpan.FromSeconds(stage.Duration.Next(_random));
            comp.NextUpdate = now + duration;

            var (stageWeather, stageMessage) = PickVariant(stage); // ADT-Tweak
            var mapId = Comp<MapComponent>(map).MapId;
            if (stageWeather is {} weather) // ADT-Tweak
            {
                if (HasWeather(comp, comp.Stage - 1))
                    duration += SharedWeatherSystem.StartupTime;
                if (HasWeather(comp, comp.Stage + 1))
                    duration += SharedWeatherSystem.ShutdownTime;
                _weather.TryAddWeather(map, weather, out _, duration);
            }

            if (stageMessage is {} message) // ADT-Tweak
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
    }

    // ADT-Tweak-Start
    private (EntProtoId? Weather, LocId? Message) PickVariant(WeatherStage stage)
    {
        if (stage.Variants.Count == 0)
            return (stage.Weather, stage.Message);

        var total = 0f;
        foreach (var variant in stage.Variants)
        {
            total += MathF.Max(variant.Weight, 0f);
        }

        if (total <= 0f)
            return (stage.Weather, stage.Message);

        var roll = _random.NextFloat(total);
        foreach (var variant in stage.Variants)
        {
            roll -= MathF.Max(variant.Weight, 0f);
            if (roll <= 0f)
                return (variant.Weather, variant.Message);
        }

        var last = stage.Variants[^1];
        return (last.Weather, last.Message);
    }
    // ADT-Tweak-End

    private bool HasWeather(WeatherSchedulerComponent comp, int stage)
    {
        if (stage < 0)
            stage = comp.Stages.Count + stage;
        else if (stage >= comp.Stages.Count)
            stage %= comp.Stages.Count;

        // ADT-Tweak-Start
        var entry = comp.Stages[stage];
        return entry.Weather != null || entry.Variants.Count > 0;
        // ADT-Tweak-End
    }
}