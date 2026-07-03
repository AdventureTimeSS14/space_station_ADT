using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.AutoPostingChat;

public sealed class AutoEmotePostingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoEmotePostingChatComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<AutoEmotePostingChatComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AutoEmotePostingChatComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextFire > curTime)
                continue;

            if (!string.IsNullOrEmpty(comp.PostingMessageEmote))
            {
                _chat.TrySendInGameICMessage(uid, comp.PostingMessageEmote, InGameICChatType.Emote, ChatTransmitRange.Normal);
            }

            var nextInterval = comp.EmoteTimerRead;
            if (comp.RandomIntervalEmote)
            {
                nextInterval = _random.Next(comp.IntervalRandomEmoteMin, comp.IntervalRandomEmoteMax);
            }

            comp.NextFire += TimeSpan.FromSeconds(nextInterval);
        }
    }

    private void OnMobState(EntityUid uid, AutoEmotePostingChatComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<AutoEmotePostingChatComponent>(uid);
        }
    }

    private void OnMapInit(EntityUid uid, AutoEmotePostingChatComponent component, MapInitEvent args)
    {
        var initialDelay = component.EmoteTimerRead;
        if (component.RandomIntervalEmote)
        {
            initialDelay = _random.Next(component.IntervalRandomEmoteMin, component.IntervalRandomEmoteMax);
        }

        component.NextFire = _timing.CurTime + TimeSpan.FromSeconds(initialDelay);
    }
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
