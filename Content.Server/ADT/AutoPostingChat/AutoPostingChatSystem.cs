using Content.Server.Administration.Commands;
using Content.Shared.Mobs;
using Content.Server.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.ADT.AutoPostingChat;
using Content.Shared.Interaction.Components;
using System.Timers;
using System.ComponentModel;
using System.Linq;
using Robust.Shared.Timing;
using Robust.Shared.Random;
public sealed class AutoPostingChatSystem : EntitySystem
{
    // [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    // [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private System.Timers.Timer _speakTimer = new();
    private System.Timers.Timer _emoteTimer = new();
    //private readonly Random _random = new Random(); 
    //private static readonly Random _random = new Random();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AutoPostingChatComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<AutoPostingChatComponent, MobStateChangedEvent>(OnMobState);
        SubscribeLocalEvent<AutoPostingChatComponent, ComponentShutdown>(ComponentRemove);
    }

    private void ComponentRemove(EntityUid uid, AutoPostingChatComponent component, ComponentShutdown args)
    {
        _speakTimer.Stop();
        _emoteTimer.Stop();
        _speakTimer.Dispose(); // освобождаем ресурсы
        _emoteTimer.Dispose();
    }

    /// <summary>
    /// On death removes active comps.
    /// </summary>
    private void OnMobState(EntityUid uid, AutoPostingChatComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            RemComp<AutoPostingChatComponent>(uid);
        }
    }

    private void OnComponentStartup(EntityUid uid, AutoPostingChatComponent component, ComponentStartup args)
    {
        SetupSpeakTimer(uid, component);
        SetupEmoteTimer(uid, component);
    }

    private void SetupSpeakTimer(EntityUid uid, AutoPostingChatComponent component)
    {
        //var msg = _random.Pick(component.SpeakTimerRead);
        _speakTimer.Interval = component.RandomIntervalSpeak ? _random.Next(component.MinRandomIntervalSpeak*1000, component.MaxRandomIntervalSpeak*1000) : component.SpeakTimerRead*1000;
        _speakTimer.Elapsed += (sender, eventArgs) =>
        {
            if (component.PostingMessageSpeak != null)
            {
                _chat.TrySendInGameICMessage(uid, _random.Pick(component.PostingMessageSpeak), 
                InGameICChatType.Speak, ChatTransmitRange.Normal);
            }
            _speakTimer.Interval = component.RandomIntervalSpeak ? _random.Next(component.MinRandomIntervalSpeak*1000, component.MaxRandomIntervalSpeak*1000) : component.SpeakTimerRead*1000;
        };

        _speakTimer.Start();
    }

    private void SetupEmoteTimer(EntityUid uid, AutoPostingChatComponent component)
    {
        _emoteTimer.Interval = component.RandomIntervalEmote ? _random.Next(component.MinRandomIntervalEmote*1000, component.MaxRandomIntervalEmote*1000) : component.EmoteTimerRead*1000;
        _emoteTimer.Elapsed += (sender, eventArgs) =>
        {
            if (component.PostingMessageEmote != null)
            {
                _chat.TrySendInGameICMessage(uid, _random.Pick(component.PostingMessageEmote), 
                InGameICChatType.Emote, ChatTransmitRange.Normal);
            }
           _emoteTimer.Interval = component.RandomIntervalEmote ? _random.Next(component.MinRandomIntervalEmote*1000, component.MaxRandomIntervalEmote*1000) : component.EmoteTimerRead*1000;
        };

        _emoteTimer.Start();
    }

    // private void OnComponentStartup(EntityUid uid, AutoPostingChatComponent component, ComponentStartup args)
    // {
    //     if (component == null)
    //     {
    //         Log.Debug("AutoPostingChatComponent отсутствует на сущности с UID: " + uid);
    //         return;
    //     }

    //     _speakTimer.Interval = component.RandomIntervalSpeak ? _random.Next(1000, 30001) : component.SpeakTimerRead;
    //     _speakTimer.Elapsed += (sender, eventArgs) =>
    //     {
    //         if (component.PostingMessageSpeak != null)
    //         {
    //             _chat.TrySendInGameICMessage(uid, component.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
    //         }
    //         _speakTimer.Interval = component.RandomIntervalSpeak ? _random.Next(1000, 30001) : component.SpeakTimerRead;
    //     };

    //     _emoteTimer.Interval = component.RandomIntervalEmote ? _random.Next(1000, 30001) : component.EmoteTimerRead;
    //     _emoteTimer.Elapsed += (sender, eventArgs) =>
    //     {
    //         if (component.PostingMessageEmote != null)
    //         {
    //             _chat.TrySendInGameICMessage(uid, component.PostingMessageEmote, InGameICChatType.Emote, ChatTransmitRange.Normal);
    //         }
    //         _emoteTimer.Interval = component.RandomIntervalEmote ? _random.Next(1000, 30001) : component.EmoteTimerRead;
    //     };

    //     _speakTimer.Start();
    //     _emoteTimer.Start();
    // }
}