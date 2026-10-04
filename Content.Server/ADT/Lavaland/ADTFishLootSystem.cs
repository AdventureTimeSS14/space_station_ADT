using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTFishLootSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private PuddleSystem _puddle = default!;
    [Dependency] private ReactiveSystem _reactive = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAcidBladderComponent, ThrowDoHitEvent>(OnBladderHit);
        SubscribeLocalEvent<ADTConductiveOrganComponent, AfterInteractEvent>(OnOrganAfterInteract);
    }

    private void OnBladderHit(Entity<ADTAcidBladderComponent> ent, ref ThrowDoHitEvent args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        var target = args.Target;

        if (HasComp<MobStateComponent>(target))
        {
            var reagent = new ReagentQuantity(ent.Comp.Reagent, ent.Comp.MobAmount);
            _reactive.ReactionEntity(target, ReactionMethod.Touch, reagent);
            _popup.PopupEntity(Loc.GetString("adt-acid-bladder-burst-mob", ("target", Identity.Entity(target, EntityManager))), target, PopupType.MediumCaution);
        }
        else if (_tag.HasTag(target, WallTag))
        {
            _damageable.TryChangeDamage(target, ent.Comp.WallDamage, true);
            _popup.PopupEntity(Loc.GetString("adt-acid-bladder-burst-wall"), target, PopupType.MediumCaution);
        }
        else
        {
            var solution = new Solution(ent.Comp.Reagent, ent.Comp.FloorAmount);
            _puddle.TrySpillAt(Transform(ent.Owner).Coordinates, solution, out _);
        }

        QueueDel(ent.Owner);
    }

    private void OnOrganAfterInteract(Entity<ADTConductiveOrganComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        var tray = target;
        if (!HasComp<PlantTrayComponent>(tray))
        {
            if (!HasComp<PlantComponent>(target))
                return;

            tray = Transform(target).ParentUid;
            if (!HasComp<PlantTrayComponent>(tray))
                return;
        }

        args.Handled = true;

        if (!_plantTray.TryGetPlant(tray, out var plant) || !TryComp<PlantHolderComponent>(plant, out var holder))
        {
            _popup.PopupEntity(Loc.GetString("adt-conductive-organ-no-seed"), target, args.User);
            return;
        }

        holder.YieldMod = ent.Comp.YieldMod;
        Dirty(plant.Value, holder);
        _plantTray.AdjustWater(tray, 100f);
        _plantTray.AdjustNutrient(tray, 100f);

        _popup.PopupEntity(Loc.GetString("adt-conductive-organ-used", ("target", tray)), tray, args.User);
        QueueDel(ent.Owner);
    }
}
