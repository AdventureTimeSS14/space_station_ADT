using System.Linq;
using Content.Server.ADT.Dice.Components;
using Content.Shared.Examine;
using Content.Shared.Gibbing;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Systems;
using Content.Server.Antag;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.ADT.Dice;
using Content.Shared.Dice;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.PDA;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Roles.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Mind;

namespace Content.Server.ADT.Dice;

public sealed class DiceOfFateSystem : EntitySystem
{
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly ProtoId<DamageModifierSetPrototype> DamageMod = "DiceOfFateMod";
    private static readonly ProtoId<PolymorphPrototype> Monkey = "Monkey";
    private static readonly ProtoId<DamageTypePrototype> Asphyxiation = "Asphyxiation";

    private static readonly EntProtoId RandomAggressive = "RandomAggressiveAnimal";
    private static readonly EntProtoId RandomSpellbook = "RandomSpellbook";
    private static readonly EntProtoId Revolver = "WeaponRevolverInspector";
    private static readonly EntProtoId DiceOfFateWizardRule = "DiceOfFateWizardRule";
    private static readonly EntProtoId Cookie = "FoodBakedCookie";
    private static readonly EntProtoId RandomImplanter = "RandomDiceImplanter";
    private static readonly EntProtoId ThiefToolbox = "ToolboxThief";
    private static readonly EntProtoId Cash = "SpaceCash10000";
    private const string DiceOfFateWizardObjectiveProto = "DiceOfFateWizardObjective";

    private ProtoId<AccessLevelPrototype>[]? _allAccess;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiceOfFateComponent, UseInHandEvent>(OnUseInHand, after: [typeof(SharedDiceSystem)]);
        SubscribeLocalEvent<DiceOfFateComponent, LandEvent>(OnLand, after: [typeof(SharedDiceSystem)]);
        SubscribeLocalEvent<DiceOfFateComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInHand(Entity<DiceOfFateComponent> entity, ref UseInHandEvent args)
    {
        if (!TryComp<DiceComponent>(entity, out var dice))
            return;

        // Проверяем, бросал ли этот игрок уже
        if (HasComp<DiceOfFateUserComponent>(args.User))
            return;

        if (entity.Comp.RollsLeft <= 0)
            return;

        entity.Comp.RollsLeft--;
        RollFate(args.User, dice.CurrentValue);
        EnsureComp<DiceOfFateUserComponent>(args.User);
    }

    private void OnLand(Entity<DiceOfFateComponent> entity, ref LandEvent args)
    {
        if (args.User == null || !TryComp<DiceComponent>(entity, out var dice))
            return;

        var user = args.User.Value;

        // Проверяем, бросал ли этот игрок уже
        if (HasComp<DiceOfFateUserComponent>(user))
            return;

        if (entity.Comp.RollsLeft <= 0)
            return;

        entity.Comp.RollsLeft--;
        RollFate(user, dice.CurrentValue);
        EnsureComp<DiceOfFateUserComponent>(user);
    }

    private void OnExamined(Entity<DiceOfFateComponent> entity, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(DiceOfFateComponent)))
        {
            args.PushMarkup(Loc.GetString("dice-of-fate-examine", ("rollsLeft", entity.Comp.RollsLeft)));
        }
    }

    public void RollFate(EntityUid user, int value)
    {
        var pos = _transform.GetMapCoordinates(user);

        var ok = value switch
        {
            1 => Gib(user),
            2 => Kill(user),
            3 => SummonMobs(pos),
            4 => DestroyItems(user),
            5 => _polymorph.PolymorphEntity(user, Monkey) != null,
            6 => SlowDown(user),
            7 => StunAndDamage(user),
            8 => Explode(user),
            9 => Nothing(user),
            10 => Nothing(user),
            11 => SpawnInHand(user, Cookie, pos),
            12 => Heal(user),
            13 => SpawnInHand(user, Cash, pos),
            14 => SpawnInHand(user, Revolver, pos),
            15 => SpawnInHand(user, RandomSpellbook, pos),
            16 => SpawnInHand(user, RandomImplanter, pos),
            17 => SpawnInHand(user, ThiefToolbox, pos),
            18 => FullAccess(user),
            19 => Resist50(user),
            20 => BecomeWizard(user),
            _ => Nothing(user)
        };

        _admin.Add(LogType.Action, LogImpact.Extreme,
            $"{ToPrettyString(user):user} rolled Dice of Fate and got {value} (success: {ok})");
    }

    private bool Gib(EntityUid user)
    {
        _gibbing.Gib(user);
        return true;
    }

    private bool Kill(EntityUid user)
    {
        _damage.TryChangeDamage((user, null), new DamageSpecifier { DamageDict = { { Asphyxiation, 400 } } }, ignoreResistances: true);
        return true;
    }

    private bool SummonMobs(MapCoordinates pos)
    {
        var count = _random.Next(3, 6);
        for (var i = 0; i < count; i++)
            Spawn(RandomAggressive, pos);

        return true;
    }

    private bool DestroyItems(EntityUid user)
    {
        if (_inventory.TryGetSlots(user, out var slots))
        {
            foreach (var slot in slots)
            {
                if (_inventory.TryGetSlotEntity(user, slot.Name, out var item))
                    QueueDel(item);
            }
        }

        if (TryComp<HandsComponent>(user, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld((user, hands)).ToList())
                QueueDel(held);
        }

        return true;
    }

    private bool SlowDown(EntityUid user)
    {
        if (!TryComp(user, out MovementSpeedModifierComponent? move))
            return false;

        var mult = _random.NextFloat(0.3f, 0.95f);
        _speed.ChangeBaseSpeed(user, move.BaseWalkSpeed * mult, move.BaseSprintSpeed * mult, move.Acceleration, move);
        return true;
    }

    private bool StunAndDamage(EntityUid user)
    {
        _stun.TryKnockdown(user, TimeSpan.FromSeconds(30));
        _damage.TryChangeDamage((user, null), new DamageSpecifier { DamageDict = { { Asphyxiation, 50 } } }, ignoreResistances: true);
        return true;
    }

    private bool Explode(EntityUid user)
    {
        _explosion.QueueExplosion(user, ExplosionSystem.DefaultExplosionPrototypeId, 5000f, 3f, 10f);
        return true;
    }

    private bool Nothing(EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("reagent-desc-nothing"), user, user);
        return true;
    }

    private bool Heal(EntityUid user)
    {
        _rejuvenate.PerformRejuvenate(user);
        return true;
    }

    private bool SpawnInHand(EntityUid user, EntProtoId proto, MapCoordinates pos, bool pickUp = true)
    {
        var ent = Spawn(proto, pos);
        if (pickUp)
            _hands.TryForcePickupAnyHand(user, ent);
        return true;
    }

    private bool FullAccess(EntityUid user)
    {
        var id = FindActiveId(user);
        if (id == null)
            return false;

        _allAccess ??= _prototype
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Select(p => new ProtoId<AccessLevelPrototype>(p.ID))
            .ToArray();

        _access.TrySetTags(id.Value, _allAccess);
        return true;
    }

    private bool Resist50(EntityUid user)
    {
        if (!HasComp<DamageableComponent>(user))
            return false;

        _damage.SetDamageModifierSetId((user, null), DamageMod);
        return true;
    }

    private bool BecomeWizard(EntityUid user)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        _antag.ForceMakeAntag<WizardRoleComponent>(actor.PlayerSession, DiceOfFateWizardRule);

        if (_mind.TryGetMind(user, out var mindId, out var mindComp))
        {
            _mind.TryAddObjective(mindId, mindComp, DiceOfFateWizardObjectiveProto);
        }

        return true;
    }

    private EntityUid? FindActiveId(EntityUid target)
    {
        if (_inventory.TryGetSlotEntity(target, "id", out var slotEntity))
        {
            if (HasComp<AccessComponent>(slotEntity))
                return slotEntity.Value;

            if (TryComp<PdaComponent>(slotEntity, out var pda) && HasComp<IdCardComponent>(pda.ContainedId))
                return pda.ContainedId;
        }
        else if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld((target, hands)))
            {
                if (HasComp<AccessComponent>(held))
                    return held;
            }
        }

        return null;
    }
}