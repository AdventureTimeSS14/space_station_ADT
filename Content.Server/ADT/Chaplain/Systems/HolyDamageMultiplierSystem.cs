using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Chaplain;

public sealed class HolyDamageMultiplierSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    // Идентификатор типа святого урона
    private const string HolyDamageType = "Holy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyDamageMultiplierComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, HolyDamageMultiplierComponent component, ref DamageModifyEvent args)
    {
        // Проверяем наличие прототипа святого урона
        if (!_prototype.HasIndex<DamageTypePrototype>(HolyDamageType))
            return;

        // Проверяем наличие святого урона в повреждениях
        if (!args.Damage.DamageDict.TryGetValue(HolyDamageType, out var holyDamage))
            return;

        // Применяем множитель
        var multipliedDamage = holyDamage * component.Multiplier;
        args.Damage.DamageDict[HolyDamageType] = multipliedDamage;
    }
}