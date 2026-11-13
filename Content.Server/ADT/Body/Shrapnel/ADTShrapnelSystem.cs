using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.ADT.Body.Bones;
using Content.Server.ADT.Surgery;
using Content.Shared.ADT.Body.Shrapnel;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Trigger;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body.Shrapnel;

public sealed class ADTShrapnelSystem : XOnTriggerSystem<ADTShrapnelOnTriggerComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ADTFractureSystem _fracture = default!;
    [Dependency] private SurgerySystem _surgery = default!;
    [Dependency] private AlertsSystem _alerts = default!;

    private static readonly ProtoId<AlertPrototype> ShrapnelAlert = "ADTEmbeddedShrapnel";

    private static readonly ProtoId<OrganCategoryPrototype> Torso = "Torso";
    private static readonly ProtoId<SurgeryToolQualityPrototype>[] ExtractQualities = { "Clamping", "Incision" };
    private static readonly TimeSpan ToolRemovalTime = TimeSpan.FromSeconds(4);

    private static readonly DamageSpecifier ToolRemovalDamage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 3 },
        },
    };

    private static readonly DamageSpecifier ToolFailDamage = new()
    {
        DamageDict = new()
        {
            { "Piercing", 6 },
        },
    };

    private static readonly Dictionary<ProtoId<OrganCategoryPrototype>, float> BlastZones = new()
    {
        ["Torso"] = 3f,
        ["Head"] = 1f,
        ["ArmLeft"] = 1f,
        ["ArmRight"] = 1f,
        ["HandLeft"] = 1f,
        ["HandRight"] = 1f,
        ["LegLeft"] = 1f,
        ["LegRight"] = 1f,
        ["FootLeft"] = 1f,
        ["FootRight"] = 1f,
    };

    private readonly HashSet<Entity<BodyComponent>> _bodies = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTShrapnelOnHitComponent, ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<ADTEmbeddedObjectsComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<ADTEmbeddedCarrierComponent, ADTShrapnelRemoveDoAfterEvent>(OnRemoveDoAfter);
        SubscribeLocalEvent<ADTEmbeddedCarrierComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ADTEmbeddedCarrierComponent, ComponentStartup>(OnCarrierStartup);
        SubscribeLocalEvent<ADTEmbeddedCarrierComponent, ComponentShutdown>(OnCarrierShutdown);
    }

    protected override void OnTrigger(Entity<ADTShrapnelOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var origin = _transform.GetMapCoordinates(ent.Owner);

        _bodies.Clear();
        _lookup.GetEntitiesInRange(origin, ent.Comp.Range, _bodies);

        foreach (var body in _bodies)
        {
            var other = _transform.GetMapCoordinates(body.Owner);
            var uid = body.Owner;
            if (!_interaction.InRangeUnobstructed(origin, other, ent.Comp.Range, predicate: e => e == uid || e == ent.Owner))
                continue;

            var amount = ent.Comp.MaxShrapnel - (int) MathF.Floor((other.Position - origin.Position).Length());
            for (var i = 0; i < amount; i++)
            {
                if (_fracture.PickZone(BlastZones) is not { } zone)
                    continue;

                if (_random.Prob(GetEmbedChance(uid, zone, ent.Comp.EmbedChance)))
                    Embed(uid, zone, ent.Comp.Shrapnel);
            }
        }

        args.Handled = true;
    }

    private void OnProjectileHit(Entity<ADTShrapnelOnHitComponent> ent, ref ProjectileHitEvent args)
    {
        if (!HasComp<BodyComponent>(args.Target) || _fracture.PickZone(ent.Comp.ZoneWeights) is not { } zone)
            return;

        if (_random.Prob(GetEmbedChance(args.Target, zone, ent.Comp.EmbedChance)))
            Embed(args.Target, zone, ent.Comp.Shrapnel);
    }

    private float GetEmbedChance(EntityUid body, ProtoId<OrganCategoryPrototype> zone, float chance)
    {
        var probe = new DamageSpecifier
        {
            DamageDict = new()
            {
                { "Piercing", 100 },
            },
        };

        var after = _fracture.ApplyZoneArmor(body, zone, probe).GetTotal().Float() / 100f;
        return Math.Clamp(chance * after, 0f, 1f);
    }

    public bool TryGetOrgan(EntityUid body, ProtoId<OrganCategoryPrototype> zone, [NotNullWhen(true)] out EntityUid? organ)
    {
        organ = null;
        foreach (var candidate in _body.EnumerateOrgans<OrganComponent>(body))
        {
            if (candidate.Comp1.Category == zone)
            {
                organ = candidate.Owner;
                return true;
            }

            if (candidate.Comp1.Category == Torso)
                organ ??= candidate.Owner;
        }

        return organ != null;
    }

    public bool Embed(EntityUid body, ProtoId<OrganCategoryPrototype> zone, EntProtoId shrapnel, bool silent = false)
    {
        if (!TryGetOrgan(body, zone, out var organ))
            return false;

        EnsureComp<ADTEmbeddedObjectsComponent>(organ.Value);
        var container = _container.EnsureContainer<Container>(organ.Value, ADTEmbeddedObjectsComponent.ContainerId);

        var item = Spawn(shrapnel, _transform.GetMapCoordinates(body));
        if (!_container.Insert(item, container))
        {
            QueueDel(item);
            return false;
        }

        EnsureCarrier(body);

        if (TryComp<ADTEmbeddableShrapnelComponent>(item, out var embeddable) && !embeddable.ImpactDamage.Empty)
            _damageable.TryChangeDamage(body, embeddable.ImpactDamage, interruptsDoAfters: false);

        if (!silent)
            _popup.PopupEntity(Loc.GetString("adt-shrapnel-embed-self", ("item", item), ("limb", organ.Value)), body, body, PopupType.MediumCaution);

        return true;
    }

    private void EnsureCarrier(EntityUid body)
    {
        if (HasComp<ADTEmbeddedCarrierComponent>(body))
            return;

        var carrier = EnsureComp<ADTEmbeddedCarrierComponent>(body);
        carrier.NextUpdate = _timing.CurTime + carrier.UpdateInterval;
    }

    private void OnOrganInserted(Entity<ADTEmbeddedObjectsComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (GetEmbedded(ent.Owner).Count > 0)
            EnsureCarrier(args.Target);
    }

    public List<EntityUid> GetEmbedded(EntityUid organ)
    {
        if (!_container.TryGetContainer(organ, ADTEmbeddedObjectsComponent.ContainerId, out var container))
            return new List<EntityUid>();

        return container.ContainedEntities.ToList();
    }

    public IEnumerable<(EntityUid Organ, List<EntityUid> Items)> EnumerateEmbedded(EntityUid body)
    {
        foreach (var organ in _body.EnumerateOrgans<ADTEmbeddedObjectsComponent>(body))
        {
            var items = GetEmbedded(organ.Owner);
            if (items.Count > 0)
                yield return (organ.Owner, items);
        }
    }

    public bool HasEmbedded(EntityUid body, ProtoId<OrganCategoryPrototype> zone)
    {
        foreach (var organ in _body.EnumerateOrgans<ADTEmbeddedObjectsComponent>(body))
        {
            if (organ.Comp1.Category == zone && GetEmbedded(organ.Owner).Count > 0)
                return true;
        }

        return false;
    }

    public int RemoveAll(EntityUid body, ProtoId<OrganCategoryPrototype> zone, EntityUid? user)
    {
        var removed = 0;
        foreach (var organ in _body.EnumerateOrgans<ADTEmbeddedObjectsComponent>(body).ToList())
        {
            if (organ.Comp1.Category != zone)
                continue;

            foreach (var item in GetEmbedded(organ.Owner))
            {
                Extract(body, item, user);
                removed++;
            }
        }

        return removed;
    }

    private void Extract(EntityUid body, EntityUid item, EntityUid? user)
    {
        _transform.DropNextTo(item, body);

        if (user != null)
            _hands.TryPickupAnyHand(user.Value, item);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTEmbeddedCarrierComponent>();
        while (query.MoveNext(out var uid, out var carrier))
        {
            if (now < carrier.NextUpdate)
                continue;

            carrier.NextUpdate = now + carrier.UpdateInterval;

            var any = false;
            foreach (var (organ, items) in EnumerateEmbedded(uid).ToList())
            {
                any = true;

                if (_mobState.IsDead(uid))
                    continue;

                foreach (var item in items)
                {
                    if (!TryComp<ADTEmbeddableShrapnelComponent>(item, out var shrapnel))
                        continue;

                    if (_random.Prob(shrapnel.PainChance))
                    {
                        _damageable.TryChangeDamage(uid, shrapnel.PainDamage, interruptsDoAfters: false);
                        _popup.PopupEntity(Loc.GetString("adt-shrapnel-pain", ("item", item), ("limb", organ)), uid, uid, PopupType.SmallCaution);
                    }

                    if (!_random.Prob(shrapnel.FallChance))
                        continue;

                    Extract(uid, item, null);
                    _damageable.TryChangeDamage(uid, shrapnel.FallDamage, interruptsDoAfters: false);
                    _popup.PopupEntity(Loc.GetString("adt-shrapnel-fall", ("item", item), ("target", uid)), uid, PopupType.MediumCaution);
                }
            }

            if (!any)
                RemCompDeferred<ADTEmbeddedCarrierComponent>(uid);
        }
    }

    public bool IsExtractTool(EntityUid item, out float speed, out float success)
    {
        foreach (var quality in ExtractQualities)
        {
            if (_surgery.TryMatchToolQuality(quality, item, out speed, out success))
                return true;
        }

        speed = 1f;
        success = 1f;
        return false;
    }

    public void StartRemove(EntityUid user, EntityUid body, EntityUid organ, EntityUid? tool)
    {
        var items = GetEmbedded(organ);
        if (items.Count == 0)
            return;

        TimeSpan delay;
        if (tool != null && IsExtractTool(tool.Value, out var speed, out _))
            delay = ToolRemovalTime * speed;
        else
            delay = CompOrNull<ADTEmbeddableShrapnelComponent>(items[0])?.UnsafeRemovalTime ?? TimeSpan.FromSeconds(6);

        var args = new DoAfterArgs(EntityManager, user, delay, new ADTShrapnelRemoveDoAfterEvent(GetNetEntity(organ), tool != null), body, body, tool)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
            BreakOnHandChange = tool != null,
        };

        if (!_doAfter.TryStartDoAfter(args))
            return;

        _popup.PopupEntity(Loc.GetString("adt-shrapnel-remove-start", ("user", user), ("limb", organ)), body, PopupType.Medium);
    }

    private void OnRemoveDoAfter(Entity<ADTEmbeddedCarrierComponent> ent, ref ADTShrapnelRemoveDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var organ = GetEntity(args.Organ);
        if (CompOrNull<OrganComponent>(organ)?.Body != ent.Owner)
            return;

        var items = GetEmbedded(organ);
        if (items.Count == 0)
            return;

        var item = items[0];

        if (args.WithTool)
        {
            if (args.Used is not { } tool || !IsExtractTool(tool, out _, out var success))
                return;

            if (!_random.Prob(success))
            {
                _damageable.TryChangeDamage(ent.Owner, ToolFailDamage, interruptsDoAfters: false, origin: args.User);
                _popup.PopupEntity(Loc.GetString("adt-shrapnel-remove-fail", ("limb", organ)), ent, args.User, PopupType.MediumCaution);
                _fracture.TryScream(ent);
                return;
            }

            Extract(ent, item, args.User);
            _damageable.TryChangeDamage(ent.Owner, ToolRemovalDamage, interruptsDoAfters: false, origin: args.User);
        }
        else
        {
            var damage = CompOrNull<ADTEmbeddableShrapnelComponent>(item)?.UnsafeRemovalDamage;
            Extract(ent, item, args.User);
            if (damage != null)
                _damageable.TryChangeDamage(ent.Owner, damage, interruptsDoAfters: false, origin: args.User);

            _fracture.TryScream(ent);
        }

        _popup.PopupEntity(Loc.GetString("adt-shrapnel-removed", ("user", args.User), ("item", item), ("limb", organ)), ent, PopupType.Medium);
    }

    private void OnCarrierStartup(Entity<ADTEmbeddedCarrierComponent> ent, ref ComponentStartup args)
    {
        _alerts.ShowAlert(ent.Owner, ShrapnelAlert);
    }

    private void OnCarrierShutdown(Entity<ADTEmbeddedCarrierComponent> ent, ref ComponentShutdown args)
    {
        _alerts.ClearAlert(ent.Owner, ShrapnelAlert);
    }

    private void OnRejuvenate(Entity<ADTEmbeddedCarrierComponent> ent, ref RejuvenateEvent args)
    {
        foreach (var (_, items) in EnumerateEmbedded(ent).ToList())
        {
            foreach (var item in items)
            {
                QueueDel(item);
            }
        }

        RemCompDeferred<ADTEmbeddedCarrierComponent>(ent);
    }
}
