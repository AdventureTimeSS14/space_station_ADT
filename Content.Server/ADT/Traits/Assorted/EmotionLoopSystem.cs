using Content.Server.Chat.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Traits.Assorted;

/// <summary>
/// This system allows triggering any emotion at random intervals.
/// </summary>
public sealed class EmotionLoopSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EmotionLoopComponent, ComponentStartup>(SetupTimer);
    }

    private void SetupTimer(Entity<EmotionLoopComponent> entity, ref ComponentStartup args)
    {
        var delaySeconds = _random.Next((int)entity.Comp.MinTimeBetweenEmotions.TotalSeconds, (int)entity.Comp.MaxTimeBetweenEmotions.TotalSeconds);
        entity.Comp.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(delaySeconds);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<EmotionLoopComponent>();
        while (query.MoveNext(out var uid, out var emotionLoop))
        {
            if (emotionLoop.Emotes.Count == 0)
                return;

            if (_timing.CurTime < emotionLoop.NextIncidentTime)
                continue;

            emotionLoop.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.Next((int)emotionLoop.MinTimeBetweenEmotions.TotalSeconds, (int)emotionLoop.MaxTimeBetweenEmotions.TotalSeconds));

            // Play the emotion by random index.
            _chat.TryEmoteWithChat(uid, emotionLoop.Emotes[_random.Next(0, emotionLoop.Emotes.Count)], ignoreActionBlocker: false);
        }
    }
}
