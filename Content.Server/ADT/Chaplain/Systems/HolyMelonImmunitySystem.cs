using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Chaplain.Systems;

/// <summary>
/// Система отслеживает предметы с <see cref="HolyMelonImmunityComponent"/> в руках игроков
/// и выдаёт им временный компонент <see cref="HolyMelonMagicImmunityComponent"/> для иммунитета к магии.
/// Иммунитет работает только пока предмет находится в руке.
/// </summary>
public sealed class HolyMelonImmunitySystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Подписываемся на события экипировки/снятия арбуза
        SubscribeLocalEvent<HolyMelonImmunityComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<HolyMelonImmunityComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
    }

    private void OnGotEquippedHand(EntityUid uid, HolyMelonImmunityComponent component, GotEquippedHandEvent args)
    {
        // НЕ добавляем иммунитет священникам — у них он уже есть врождённый
        if (HasComp<ChaplainComponent>(args.User))
            return;

        // Добавляем временный маркер иммунитета от арбуза
        EnsureComp<HolyMelonMagicImmunityComponent>(args.User);

        // Основной компонент иммунитета (если ещё не добавлен)
        EnsureComp<MagicImmunityComponent>(args.User);
    }

    private void OnGotUnequippedHand(EntityUid uid, HolyMelonImmunityComponent component, GotUnequippedHandEvent args)
    {
        // НЕ снимаем иммунитет у священников — у них он врождённый
        if (HasComp<ChaplainComponent>(args.User))
            return;

        RemoveImmunityIfNoOtherMelons(args.User, uid);
    }

    private void RemoveImmunityIfNoOtherMelons(EntityUid holder, EntityUid? excludeItem = null)
    {
        if (!HasComp<HolyMelonMagicImmunityComponent>(holder))
            return;

        if (!TryComp<HandsComponent>(holder, out var hands))
            return;

        foreach (var hand in _hands.EnumerateHeld((holder, hands)))
        {
            // Исключаем предмет, который снимается
            if (excludeItem.HasValue && hand == excludeItem.Value)
                continue;

            if (HasComp<HolyMelonImmunityComponent>(hand))
                return;
        }

        // Нет других арбузов в руках - удаляем временный маркер
        RemComp<HolyMelonMagicImmunityComponent>(holder);

        // Удаляем основной компонент иммунитета, только если нет других источников
        // (священник имеет ChaplainComponent, который не удаляется)
        if (!HasComp<ChaplainComponent>(holder))
        {
            RemComp<MagicImmunityComponent>(holder);
        }
    }
}
