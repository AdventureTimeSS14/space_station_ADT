using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Alert;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Systems;
using Content.Shared.Chat;
using Content.Shared.Mobs.Components;

namespace Content.Server.ADT.Heretic.Systems.Moon;

[RegisterComponent]
public sealed partial class MoonRegaliaWearerComponent : Component
{
}

public sealed partial class MoonRegaliaSystem : EntitySystem
{
    private static readonly ProtoId<AlertPrototype> MoonBrain = "MoonBrain";

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;

    private static readonly HashSet<string> DisableKeys = new()
    {
        "Stunned",
        "KnockedDown",
        "Paralyzed",
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MoonRegaliaWearerComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<MoonRegaliaWearerComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<MoonRegaliaWearerComponent, MobStateChangedEvent>(OnWearerDeath);
        SubscribeLocalEvent<MoonRegaliaWearerComponent, BeforeStatusEffectAddedEvent>(OnBeforeNewStatus);
        SubscribeLocalEvent<MoonRegaliaWearerComponent, BeforeOldStatusEffectAddedEvent>(OnBeforeOldStatus);
        SubscribeLocalEvent<KnockedDownComponent, ComponentStartup>(OnKnockedStartup);
        SubscribeLocalEvent<AttemptPacifiedAttackEvent>(OnPacifiedAttack);
        SubscribeLocalEvent<GotEquippedEvent>(OnArmorEquipped);
        SubscribeLocalEvent<GotUnequippedEvent>(OnArmorUnequipped);
    }

    private bool IsMoonArmor(EntityUid uid)
    {
        return MetaData(uid).EntityPrototype?.ID == "ADTClothingOuterArmorHereticMoon";
    }

    private void OnArmorEquipped(GotEquippedEvent args)
    {
        if (args.Slot != "outerClothing")
            return;
        if (!IsMoonArmor(args.Equipment))
            return;
        EnsureComp<PacifiedComponent>(args.EquipTarget);
        EnsureComp<MoonRegaliaWearerComponent>(args.EquipTarget);
    }

    private void OnArmorUnequipped(GotUnequippedEvent args)
    {
        if (args.Slot != "outerClothing")
            return;
        if (!IsMoonArmor(args.Equipment))
            return;
        RemCompDeferred<PacifiedComponent>(args.EquipTarget);
        RemCompDeferred<MoonRegaliaWearerComponent>(args.EquipTarget);
        if (_brainDamage.Remove(args.EquipTarget))
            _alerts.ClearAlert((args.EquipTarget, null), MoonBrain);
    }

    private bool IsHereticOrGhoulOrLunatic(EntityUid uid)
    {
        if (HasComp<MoonLunaticComponent>(uid))
            return true;
        return _heretic.IsHereticOrGhoul(uid);
    }

    private bool WearsRegalia(EntityUid uid)
    {
        return HasComp<MoonRegaliaWearerComponent>(uid);
    }

    private static readonly float[] BrainThresholds = { 0f, 21f, 51f, 101f, 151f, 190f };

    private readonly Dictionary<EntityUid, float> _brainDamage = new();

    private void OnDamageChanged(Entity<MoonRegaliaWearerComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null)
            return;

        var delta = args.DamageDelta.DamageDict.TryGetValue("Cellular", out var cellular)
            ? cellular.Float()
            : 0f;

        var total = _brainDamage.GetValueOrDefault(ent.Owner) + delta;
        if (total < 0f)
            total = 0f;
        _brainDamage[ent.Owner] = total;

        _alerts.ShowAlert((ent.Owner, null), MoonBrain, BrainSeverity(total));
    }

    private static short BrainSeverity(float damage)
    {
        short severity = 1;
        for (var i = BrainThresholds.Length - 1; i >= 0; i--)
        {
            if (damage >= BrainThresholds[i])
            {
                severity = (short) (i + 1);
                break;
            }
        }

        return severity;
    }

    private void OnBeforeDamage(Entity<MoonRegaliaWearerComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Damage.Empty)
            return;

        var total = args.Damage.GetTotal().Float() * 1.15f;
        if (total <= 0f)
            return;

        args.Damage = new DamageSpecifier
        {
            DamageDict =
            {
                { "Cellular", total },
            },
        };
    }

    private void OnWearerDeath(Entity<MoonRegaliaWearerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        _gibbing.Gib(ent);
    }

    private void OnKnockedStartup(Entity<KnockedDownComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<MoonRegaliaWearerComponent>(ent.Owner))
            RemCompDeferred<KnockedDownComponent>(ent);
    }

    private void OnBeforeNewStatus(Entity<MoonRegaliaWearerComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Effect.Id != "StatusEffectStunned")
            return;
        args.Cancelled = true;
    }

    private void OnBeforeOldStatus(Entity<MoonRegaliaWearerComponent> ent, ref BeforeOldStatusEffectAddedEvent args)
    {
        if (args.Cancelled)
            return;
        if (!DisableKeys.Contains(args.EffectKey))
            return;
        args.Cancelled = true;
    }

    private TimeSpan _nextBurn = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextBurn)
            return;
        _nextBurn = _timing.CurTime + TimeSpan.FromSeconds(2);

        var query = EntityQueryEnumerator<MoonRegaliaWearerComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (IsHereticOrGhoulOrLunatic(uid))
                continue;

            var spec = new DamageSpecifier();
            spec.DamageDict.Add("Cellular", 20f);
            _damage.TryChangeDamage(uid, spec, true);
            _chat.TryEmoteWithoutChat(uid, "Laugh");
        }
    }
    private void OnPacifiedAttack(ref AttemptPacifiedAttackEvent args)
    {
        var user = args.User;
        if (!WearsRegalia(user))
            return;

        var held = _hands.GetActiveItem(user);
        if (held == null)
        {
            args.Cancelled = true;
            return;
        }

        if (HasComp<MoonAmuletComponent>(held.Value))
            return;

        if (TryComp<HereticBladeComponent>(held.Value, out var blade) && blade.Path == "Moon")
        {
            if (_inventory.TryGetSlotEntity(user, "neck", out var neck)
                && HasComp<MoonAmuletComponent>(neck.Value))
                return;
        }

        args.Cancelled = true;
    }
}
