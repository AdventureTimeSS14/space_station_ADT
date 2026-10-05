using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Popups;

namespace Content.Server.ADT.Heretic.Systems;

public sealed partial class FleshOrganFeastSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OrganComponent, IngestedEvent>(OnOrganEaten);
    }

    private void OnOrganEaten(Entity<OrganComponent> ent, ref IngestedEvent args)
    {
        var eater = args.User;
        if (!_heretic.TryGetHereticComponent(eater, out var heretic, out _))
            return;
        if (heretic.CurrentPath != "Flesh")
            return;
        if (!TryComp<DamageableComponent>(eater, out _))
            return;

        var heal = new DamageSpecifier();
        heal.DamageDict.Add("Cellular", -10f);
        heal.DamageDict.Add("Blunt", -5f);

        _damage.TryChangeDamage(eater, heal, true);
        _popup.PopupEntity(Loc.GetString("heretic-flesh-eat-organ"), eater, eater);
    }
}