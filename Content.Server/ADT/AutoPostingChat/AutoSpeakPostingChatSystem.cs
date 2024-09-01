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
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _time = default!;
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
        if (args.NewMobState == MobState.Dead || component == null)
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
                if (comp.PostingMessageSpeak != null)
                {
                    _chat.TrySendInGameICMessage(uid, comp.PostingMessageSpeak, InGameICChatType.Speak, ChatTransmitRange.Normal);
                }

                comp.NextSecond = _time.CurTime + TimeSpan.FromSeconds(comp.SpeakTimerRead);
            }
        }
    }
}
