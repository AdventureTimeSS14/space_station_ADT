using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Content.Shared.HunterEye;
using Content.Shared.Damage;

namespace Content.Shared.HunterEye;

public sealed class HunterEyeDamageReductionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HunterEyeDamageReductionComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, HunterEyeDamageReductionComponent comp, ref DamageModifyEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var dmgComp))
            return;

        var modify = new DamageModifierSet();
        foreach (var key in dmgComp.Damage.DamageDict.Keys)
        {
            modify.Coefficients.TryAdd(key, 0.1f);
        }
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modify);
    }
}
