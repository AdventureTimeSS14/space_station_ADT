using Content.Shared.Mobs;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Server.ADT.AutoPostingChat;

public sealed class AutoEmotePostingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoEmotePostingChatComponent, MobStateChangedEvent>(OnMobState);
    }
    /// <summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    /// </summary>
    private void OnMobState(EntityUid uid, AutoEmotePostingChatComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<AutoEmotePostingChatComponent>(uid);
        }
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AutoEmotePostingChatComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_time.CurTime >= comp.NextSecond)
            {
                var delay = comp.EmoteTimerRead;

                if (comp.PostingMessageEmote != null)
                {
                    _chat.TrySendInGameICMessage(uid, comp.PostingMessageEmote, InGameICChatType.Emote, ChatTransmitRange.Normal);
                }

                if (comp.RandomIntervalEmote)
                {
                    delay = _random.Next(comp.IntervalRandomEmoteMin, comp.IntervalRandomEmoteMax);
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
