using Content.Shared.Mobs;
using Content.Server.Chat.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Chat;

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
                string? messageToPost = null;

                if (comp.ListEmoteMessages != null && comp.ListEmoteMessages.Count > 0)
                {
                    messageToPost = _random.Pick(comp.ListEmoteMessages);
                }
                else if (!string.IsNullOrEmpty(comp.PostingMessageEmote))
                {
                    messageToPost = comp.PostingMessageEmote;
                }

                if (messageToPost != null)
                {
                    _chat.TrySendInGameICMessage(uid, messageToPost, InGameICChatType.Emote, ChatTransmitRange.Normal);
                }

                int delaySeconds;
                if (comp.RandomIntervalEmote)
                {
                    delaySeconds = _random.Next(comp.IntervalRandomEmoteMin, comp.IntervalRandomEmoteMax + 1); // +1, так как верхняя граница не включается
                }
                else
                {
                    delaySeconds = comp.EmoteTimerRead;
                }

                comp.NextSecond = _time.CurTime + TimeSpan.FromSeconds(delaySeconds);
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
