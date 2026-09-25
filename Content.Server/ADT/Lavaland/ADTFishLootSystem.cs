using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.ADT.Lavaland.Components;
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
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

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
        if (args.Handled || !args.CanReach || args.Target is not { } target || !TryComp<PlantHolderComponent>(target, out var holder))
            return;

        args.Handled = true;

        if (holder.Seed == null)
        {
            _popup.PopupEntity(Loc.GetString("adt-conductive-organ-no-seed"), target, args.User);
            return;
        }

        holder.YieldMod = ent.Comp.YieldMod;
        _plantHolder.AdjustWater(target, 100f, holder);
        _plantHolder.AdjustNutrient(target, 100f, holder);
        _plantHolder.UpdateSprite(target, holder);

        _popup.PopupEntity(Loc.GetString("adt-conductive-organ-used", ("target", target)), target, args.User);
        QueueDel(ent.Owner);
    }
}
