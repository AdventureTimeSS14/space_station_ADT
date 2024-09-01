using Content.Server.Chat.Systems;
using Content.Shared.ADT.BloodCough;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Robust.Shared.Console;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.ADT.AutoPostingChat;
public sealed class BloodCoughSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodCoughComponent, DamageChangedEvent>(OnMobStateDamage);
    }

    private void OnMobStateDamage(EntityUid uid, BloodCoughComponent component, DamageChangedEvent args)
    {
        if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
        {
            var currentDamage = damageable.TotalDamage;
            if (currentDamage > 70)
            {
                Log.Debug($"Сущность {ToPrettyString(uid)} имеет урон больше 70: {currentDamage}");
                if (TryComp<BloodCoughComponent>(uid, out var posting))
                {
                    posting.CheckCoughBlood = true;
                }
            }
            if (currentDamage <= 70)
            {
                if (TryComp<BloodCoughComponent>(uid, out var posting))
                {
                    posting.CheckCoughBlood = false;
                }
            }
        }
        else
        {
            Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента BloodCoughComponent.");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodCoughComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_time.CurTime >= comp.NextSecond)
            {
                var delay = _random.Next(comp.CoughTimeMin, comp.CoughTimeMax);
                if (comp.PostingSayDamage != null)
                {
                    if (comp.CheckCoughBlood)
                        _chat.TrySendInGameICMessage(uid, comp.PostingSayDamage, InGameICChatType.Emote, ChatTransmitRange.Normal);
                }

                comp.NextSecond = _time.CurTime + TimeSpan.FromSeconds(delay);
            }
        }
    }

    // public override void Update(float frameTime)
    // {
    //     base.Update(frameTime);
    //     var query = EntityQueryEnumerator<BloodCoughComponent>();
    //     while (query.MoveNext(out var uid, out var comp))
    //     {
    //         if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
    //         {
    //             var currentDamage = damageable.TotalDamage;

    //             if (currentDamage > 70)
    //             {
    //                 Log.Debug($"Сущность {ToPrettyString(uid)} имеет урон больше 70: {currentDamage}");
    //                 HandleLowHealth(uid);
    //             }
    //             if (currentDamage <= 70)
    //             {
    //                 if (HasComp<AutoEmotePostingChatComponent>(uid))
    //                 {
    //                     RemComp<AutoEmotePostingChatComponent>(uid);
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента BloodCoughComponent.");
    //         }
    //     }
    // }

    // private void HandleLowHealth(EntityUid uid)
    // {
    //     if (!TryComp<AutoEmotePostingChatComponent>(uid, out var posting))
    //     {
    //         posting = AddComp<AutoEmotePostingChatComponent>(uid);

    //         posting.PostingMessageEmote = "Кашляет кровью";
    //         posting.EmoteTimerRead = 15;
    //     }
    // }
}
