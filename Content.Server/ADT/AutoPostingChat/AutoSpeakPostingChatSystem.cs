using System.Threading;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Robust.Shared.Random;

namespace Content.Server.ADT.AutoPostingChat;
public sealed class AutoSpeakPostingChatSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoSpeakPostingChatComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<AutoSpeakPostingChatComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<AutoSpeakPostingChatComponent, ComponentStartup>(OnComponentStartup);
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

    private void OnComponentStartup(EntityUid uid, AutoSpeakPostingChatComponent component, ComponentStartup args)
    {
        // Перезапускаем таймер при старте компонента (например, при десериализации)
        StartSpeakTimer(uid, component);
    }

    private void OnComponentShutdown(EntityUid uid, AutoSpeakPostingChatComponent component, ComponentShutdown args)
    {
        // Отменяем таймер при удалении компонента
        if (component.TokenSource != null)
        {
            component.TokenSource.Cancel();
            component.TokenSource.Dispose();
            component.TokenSource = null;
        }
    }

    private void StartSpeakTimer(EntityUid uid, AutoSpeakPostingChatComponent component)
    {
        // Отменяем предыдущий таймер если он существует
        component.TokenSource?.Cancel();
        component.TokenSource?.Dispose();
        component.TokenSource = new CancellationTokenSource();

        var delay = component.SpeakTimerRead;
        if (component.RandomIntervalSpeak)
        {
            delay = _random.Next(component.IntervalRandomSpeakMin, component.IntervalRandomSpeakMax);
        }

        // Запускаем повторяющийся таймер
        uid.SpawnRepeatingTimer(TimeSpan.FromSeconds(delay), () => OnSpeakTimerFired(uid, component), component.TokenSource.Token);
    }

    private void OnSpeakTimerFired(EntityUid uid, AutoSpeakPostingChatComponent component)
    {
        // Проверяем, что компонент все еще существует
        if (!HasComp<AutoSpeakPostingChatComponent>(uid))
            return;

        // Отправляем сообщение
        if (component.PostingMessageSpeak != null)
        {
            _chat.TrySendInGameICMessage(uid, component.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
        }

        // Если используется случайный интервал, перезапускаем таймер с новым случайным значением
        if (component.RandomIntervalSpeak)
        {
            StartSpeakTimer(uid, component);
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
