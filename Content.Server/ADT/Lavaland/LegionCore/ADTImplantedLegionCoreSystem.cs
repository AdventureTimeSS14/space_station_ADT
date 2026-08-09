using Content.Shared.ADT.Lavaland.LegionCore;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Popups;

namespace Content.Server.ADT.Lavaland.LegionCore;

public sealed class ADTImplantedLegionCoreSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTImplantedLegionCoreComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ADTImplantedLegionCoreComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != ent.Comp.TriggerState)
            return;

        RemComp<ADTImplantedLegionCoreComponent>(ent);

        _damageable.SetAllDamage(ent.Owner, 0); // todo remake
        _popup.PopupEntity(Loc.GetString("adt-legion-core-implant-trigger"), ent, ent);
    }
}
