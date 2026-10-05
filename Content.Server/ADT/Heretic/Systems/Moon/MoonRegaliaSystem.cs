using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.Alert;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.Systems.Moon;

public sealed partial class MoonRegaliaSystem : EntitySystem
{
    private static readonly ProtoId<AlertPrototype> MoonBrain = "MoonBrain";

    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private static readonly HashSet<string> DisableKeys = new()
    {
        "Stunned",
        "KnockedDown",
        "Paralyzed",
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageableComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<DamageableComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<DamageableComponent, BeforeStatusEffectAddedEvent>(OnBeforeNewStatus);
        SubscribeLocalEvent<DamageableComponent, BeforeOldStatusEffectAddedEvent>(OnBeforeOldStatus);
        SubscribeLocalEvent<KnockedDownComponent, ComponentStartup>(OnKnockedStartup);
        SubscribeLocalEvent<AttemptPacifiedAttackEvent>(OnPacifiedAttack);
    }

    private bool WearsRegalia(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "outerClothing", out var cloth))
            return false;
        return MetaData(cloth.Value).EntityPrototype?.ID == "ADTClothingOuterArmorHereticMoon";
    }

    private static readonly float[] BrainThresholds = { 0f, 21f, 51f, 101f, 151f, 190f };

    private readonly Dictionary<EntityUid, float> _brainDamage = new();

    private void OnDamageChanged(Entity<DamageableComponent> ent, ref DamageChangedEvent args)
    {
        if (!WearsRegalia(ent))
        {
            if (_brainDamage.Remove(ent))
                _alerts.ClearAlert((ent.Owner, null), MoonBrain);
            return;
        }

        if (args.DamageDelta == null)
            return;

        var delta = args.DamageDelta.DamageDict.TryGetValue("Cellular", out var cellular)
            ? cellular.Float()
            : 0f;

        var total = _brainDamage.GetValueOrDefault(ent) + delta;
        if (total < 0f)
            total = 0f;
        _brainDamage[ent] = total;

        _alerts.ShowAlert((ent.Owner, null), MoonBrain, BrainSeverity(total));
    }

    private static short BrainSeverity(float damage)
    {
        short severity = 0;
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

    private void OnBeforeDamage(Entity<DamageableComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Damage.Empty)
            return;

        var total = args.Damage.GetTotal().Float();
        if (total <= 0f)
            return;

        if (!WearsRegalia(ent))
            return;

        args.Damage = new DamageSpecifier
        {
            DamageDict =
            {
                { "Cellular", total },
            },
        };
    }

    private void OnKnockedStartup(Entity<KnockedDownComponent> ent, ref ComponentStartup args)
    {
        if (WearsRegalia(ent))
            RemCompDeferred<KnockedDownComponent>(ent);
    }

    private void OnBeforeNewStatus(Entity<DamageableComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Cancelled)
            return;
        if (args.Effect.Id != "StatusEffectStunned")
            return;
        if (!WearsRegalia(ent))
            return;
        args.Cancelled = true;
    }

    private void OnBeforeOldStatus(Entity<DamageableComponent> ent, ref BeforeOldStatusEffectAddedEvent args)
    {
        if (args.Cancelled)
            return;
        if (!DisableKeys.Contains(args.EffectKey))
            return;
        if (!WearsRegalia(ent))
            return;
        args.Cancelled = true;
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
