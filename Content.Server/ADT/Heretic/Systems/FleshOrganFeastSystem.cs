using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Heretic;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class FleshOrganFeastSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticComponent, IngestingEvent>(OnIngesting);
        SubscribeLocalEvent<HereticComponent, TryVomitEvent>(OnTryVomit);
        SubscribeLocalEvent<OrganComponent, IngestedEvent>(OnOrganEaten);
    }

    private void OnIngesting(Entity<HereticComponent> ent, ref IngestingEvent args)
    {
        if (ent.Comp.CurrentPath != "Flesh")
            return;

        if (!HasComp<OrganComponent>(args.Food))
            return;

        args.Split.RemoveReagent("UncookedAnimalProteins", args.Split.GetTotalPrototypeQuantity("UncookedAnimalProteins"));
        args.Split.RemoveReagent("Toxin", args.Split.GetTotalPrototypeQuantity("Toxin"));
    }

    private void OnTryVomit(Entity<HereticComponent> ent, ref TryVomitEvent args)
    {
        if (ent.Comp.CurrentPath != "Flesh" || args.Forced)
            return;

        args.Handled = false;
    }

    private void OnOrganEaten(Entity<OrganComponent> ent, ref IngestedEvent args)
    {
        var eater = args.User;
        if (!_heretic.TryGetHereticComponent(eater, out var heretic, out _))
            return;
        if (heretic.CurrentPath != "Flesh")
            return;
        if (!TryComp<DamageableComponent>(eater, out var damageable))
            return;

        var healable = FixedPoint2.Zero;
        foreach (var (_, amount) in damageable.Damage.DamageDict)
        {
            if (amount > FixedPoint2.Zero)
                healable += amount;
        }

        var poisonHeal = new DamageSpecifier();
        if (healable > FixedPoint2.Zero)
        {
            var budget = FixedPoint2.New(15f);
            var scale = Math.Min(1f, budget.Float() / healable.Float());
            var heal = new DamageSpecifier();
            foreach (var (type, amount) in damageable.Damage.DamageDict)
            {
                if (amount > FixedPoint2.Zero)
                {
                    heal.DamageDict[type] = amount * -scale;
                    if (type == "Poison" || type == "Caustic")
                        poisonHeal.DamageDict[type] = -amount;
                }
            }

            _damage.TryChangeDamage(eater, heal, true);
        }

        if (poisonHeal.DamageDict.Count > 0)
            _damage.TryChangeDamage(eater, poisonHeal, true);

        // Remove nausea slowdown if present
        _status.TryRemoveStatusEffect(eater, MovementModStatusSystem.VomitingSlowdown);

        _popup.PopupEntity(Loc.GetString("heretic-flesh-eat-organ"), eater, eater);
    }
}