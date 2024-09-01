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

    // public override void Initialize()
    // {
    //     base.Initialize();
    //     //SubscribeLocalEvent<BloodCoughComponent, DamageChangedEvent>(OnMobState);
    // }

    // private void OnMobState(EntityUid uid, BloodCoughComponent component, DamageChangedEvent args)
    // {
    //     if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
    //     {
    //         var currentDamage = damageable.TotalDamage;
    //         Log.Debug($"Текущее здоровье сущности {ToPrettyString(uid)}: {currentDamage}");
    //     }
    //     else
    //     {
    //         Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента DamageableComponent.");
    //     }
    // }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<BloodCoughComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
            {
                var currentDamage = damageable.TotalDamage;

                if (currentDamage > 70)
                {
                    Log.Debug($"Сущность {ToPrettyString(uid)} имеет урон больше 170: {currentDamage}");
                    HandleLowHealth(uid);
                }
                if (currentDamage <= 70)
                {
                    //добавить проверку есть ли вообще этот компонент на сущности перед тем как его удалить
                    RemComp<AutoEmotePostingChatComponent>(uid);
                }
            }
            else
            {
                Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента BloodCoughComponent.");
            }
        }
    }

    private void HandleLowHealth(EntityUid uid)
    {
        Log.Debug($"Обработка низкого здоровья для сущности {ToPrettyString(uid)}.");
        var posting = AddComp<AutoEmotePostingChatComponent>(uid);
        posting.PostingMessageEmote = "Кашляет кровью";
        posting.BloodCoughHideEmote = true;
        posting.EmoteTimerRead = 2;
    }
}
