using Content.Shared.Mobs;
using Content.Server.Chat.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Server.Emoting.Systems;
using Content.Server.Speech.EntitySystems;
using Content.Shared.ADT.AutoPostingChat;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Timers;
using System.ComponentModel;
using System.Linq;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
public sealed class AutoPostingChatSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    // public override void Initialize()
    // {
    //     base.Initialize();
    //     //SubscribeLocalEvent<AutoPostingChatComponent, ComponentStartup>(OnComponentStartup);
    //     SubscribeLocalEvent<AutoPostingChatComponent, MobStateChangedEvent>(OnMobState);
    // }
    /// <summary>
    /// On death removes active comps and gives genetic damage to prevent cloning, reduce this to allow cloning.
    /// </summary>
    // private void OnMobState(EntityUid uid, AutoPostingChatComponent component, MobStateChangedEvent args)
    // {
    //     if (args.NewMobState == MobState.Dead || component == null)
    //     {
    //         RemComp<AutoPostingChatComponent>(uid);
    //         var damageSpec = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Genetic"), 300);
    //         _damageableSystem.TryChangeDamage(uid, damageSpec);
    //     }
    // }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AutoPostingChatComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_time.CurTime < comp.NextSecond)
            {
                if (comp.PostingMessageSpeak != null)
                {
                    _chat.TrySendInGameICMessage(uid, comp.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
                }

                comp.NextSecond = _time.CurTime + TimeSpan.FromSeconds(comp.SpeakTimerRead);
            }
        }
    }

}



    // private void OnComponentStartup(EntityUid uid, AutoPostingChatComponent component, ComponentStartup args)
    // {
    //     // Проверяем наличие компонента AutoPostingChatComponent на сущности
    //     //if (component == null)
    //     //{
    //     //    Logger.Warning("AutoPostingChatComponent отсутствует на сущности с UID: " + uid);
    //     //    return;
    //     //}
    //     // Создаем таймеры для Speak и Emote
    //     var speakTimer = new System.Timers.Timer(component.SpeakTimerRead); // 8000 миллисекунд = 8 секунд по умолчанию
    //     speakTimer.Elapsed += (sender, eventArgs) =>
    //     {
    //         // Проверяем, что данные в компоненте были обновлены
    //         if (component.PostingMessageSpeak != null)
    //         {
    //             //if (component.PostingMessageSpeak == "")
    //             //    speakTimer.Stop();

    //             _chat.TrySendInGameICMessage(uid, component.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
    //         }
    //     };
    //     var emoteTimer = new System.Timers.Timer(component.EmoteTimerRead); // 9000 миллисекунд = 9 секунд по умолчанию
    //     emoteTimer.Elapsed += (sender, eventArgs) =>
    //     {
    //         // Проверяем, что данные в компоненте были обновлены
    //         if (component.PostingMessageEmote != null)
    //         {
    //             //if (component.PostingMessageEmote == "")
    //             //    emoteTimer.Stop();
    //             _chat.TrySendInGameICMessage(uid, component.PostingMessageEmote, InGameICChatType.Emote, ChatTransmitRange.Normal);
    //         }
    //     };
    //     // Запускаем таймеры
    //     speakTimer.Start();
    //     emoteTimer.Start();
    // }
