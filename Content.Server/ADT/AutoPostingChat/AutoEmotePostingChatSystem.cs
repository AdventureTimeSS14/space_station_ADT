using System.Threading;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Robust.Shared.Random;

namespace Content.Server.ADT.AutoPostingChat;

public sealed class AutoEmotePostingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoEmotePostingChatComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<AutoEmotePostingChatComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<AutoEmotePostingChatComponent, ComponentStartup>(OnComponentStartup);
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

    private void OnComponentStartup(EntityUid uid, AutoEmotePostingChatComponent component, ComponentStartup args)
    {
        StartEmoteTimer(uid, component);
    }

    private void OnComponentShutdown(EntityUid uid, AutoEmotePostingChatComponent component, ComponentShutdown args)
    {
        // Отменяем таймер при удалении компонента
        if (component.TokenSource != null)
        {
            component.TokenSource.Cancel();
            component.TokenSource.Dispose();
            component.TokenSource = null;
        }
    }

    private void StartEmoteTimer(EntityUid uid, AutoEmotePostingChatComponent component)
    {
        // Отменяем предыдущий таймер если он существует
        component.TokenSource?.Cancel();
        component.TokenSource?.Dispose();
        component.TokenSource = new CancellationTokenSource();

        var delay = component.EmoteTimerRead;
        if (component.RandomIntervalEmote)
        {
            delay = _random.Next(component.IntervalRandomEmoteMin, component.IntervalRandomEmoteMax);
        }

        // Запускаем повторяющийся таймер
        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(delay), () => OnEmoteTimerFired(uid, component), component.TokenSource.Token);
    }

    private void OnEmoteTimerFired(EntityUid uid, AutoEmotePostingChatComponent component)
    {
        // Проверяем, что компонент все еще существует
        if (!HasComp<AutoEmotePostingChatComponent>(uid))
            return;

        // Отправляем сообщение
        if (component.PostingMessageEmote != null)
        {
            _chat.TrySendInGameICMessage(uid, component.PostingMessageEmote, InGameICChatType.Emote, ChatTransmitRange.Normal);
        }

        // Если используется случайный интервал, перезапускаем таймер с новым случайным значением
        if (component.RandomIntervalEmote)
        {
            StartEmoteTimer(uid, component);
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
