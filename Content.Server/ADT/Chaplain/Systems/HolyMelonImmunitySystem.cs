using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.Bible;

/// <summary>
/// Система отслеживает предметы с <see cref="HolyMelonImmunityComponent"/> в руках игроков
/// и выдаёт им компонент <see cref="MagicImmunityComponent"/> для иммунитета к магии.
/// Иммунитет работает только пока предмет находится в руке.
/// </summary>
public sealed class HolyMelonImmunitySystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyMelonImmunityComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<HolyMelonImmunityComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
    }

    private void OnGotEquippedHand(EntityUid uid, HolyMelonImmunityComponent component, GotEquippedHandEvent args)
    {
        // НЕ добавляем иммунитет священникам — у них он уже есть врождённый
        if (HasComp<ChaplainComponent>(args.User))
            return;

        // Используем EnsureComp для предотвращения исключения при повторном добавлении
        EnsureComp<MagicImmunityComponent>(args.User);
    }

    private void OnGotUnequippedHand(EntityUid uid, HolyMelonImmunityComponent component, GotUnequippedHandEvent args)
    {
        if (HasComp<ChaplainComponent>(args.User))
            return;

        RemoveImmunityIfNoOtherMelons(args.User, uid);
    }

    private void RemoveImmunityIfNoOtherMelons(EntityUid holder, EntityUid? excludeItem = null)
    {
        if (!HasComp<MagicImmunityComponent>(holder))
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

        RemComp<MagicImmunityComponent>(holder);
    }
}
