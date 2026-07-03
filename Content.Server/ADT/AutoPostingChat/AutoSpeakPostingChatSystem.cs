using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.AutoPostingChat;
public sealed class AutoSpeakPostingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoSpeakPostingChatComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<AutoSpeakPostingChatComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<AutoSpeakPostingChatComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextFire > curTime)
                continue;

            if (!string.IsNullOrEmpty(comp.PostingMessageSpeak))
            {
                _chat.TrySendInGameICMessage(uid, comp.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
            }

            var nextInterval = comp.SpeakTimerRead;
            if (comp.RandomIntervalSpeak)
            {
                nextInterval = _random.Next(comp.IntervalRandomSpeakMin, comp.IntervalRandomSpeakMax);
            }

            comp.NextFire += TimeSpan.FromSeconds(nextInterval);
        }
    }

    private void OnMobState(EntityUid uid, AutoSpeakPostingChatComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<AutoSpeakPostingChatComponent>(uid);
        }
    }

    private void OnMapInit(EntityUid uid, AutoSpeakPostingChatComponent component, MapInitEvent args)
    {
        var initialDelay = component.SpeakTimerRead;
        if (component.RandomIntervalSpeak)
        {
            initialDelay = _random.Next(component.IntervalRandomSpeakMin, component.IntervalRandomSpeakMax);
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
