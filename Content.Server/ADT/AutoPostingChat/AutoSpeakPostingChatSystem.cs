using Content.Shared.Mobs;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.ADT.AutoPostingChat;
public sealed class AutoSpeakPostingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoSpeakPostingChatComponent, MobStateChangedEvent>(OnMobState);
    }
    /// <summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    /// </summary>
    private void OnMobState(EntityUid uid, AutoSpeakPostingChatComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<AutoSpeakPostingChatComponent>(uid);
        }
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AutoSpeakPostingChatComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_time.CurTime >= comp.NextSecond)
            {
                var delay = comp.SpeakTimerRead;

                if (comp.PostingMessageSpeak != null)
                {
                    _chat.TrySendInGameICMessage(uid, comp.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
                }

                if (comp.RandomIntervalSpeak)
                {
                    delay = _random.Next(comp.IntervalRandomSpeakMin, comp.IntervalRandomSpeakMax);
                }

                comp.NextSecond = _time.CurTime + TimeSpan.FromSeconds(delay);
            }
        }
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
