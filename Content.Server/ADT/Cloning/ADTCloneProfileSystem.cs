using Content.Server.Cloning;
using Content.Shared.Body;
using Content.Shared.Cloning.Events;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.StatusEffect;
using Content.Shared.Zombies;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Cloning;

public sealed class ADTCloneProfileSystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private GrammarSystem _grammar = default!;
    [Dependency] private IdentitySystem _identity = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IComponentFactory _compFactory = default!;

    private static readonly string[] ZombieAlteredComponents =
    [
        "Hands",
        "MeleeWeapon",
        "NpcFactionMember",
        "Puller",
        "TemperatureDamage",
    ];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, CloningEvent>(OnCloning, after: new[] { typeof(CloningSystem) });
        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnZombieStartup);
    }

    private void OnZombieStartup(Entity<ZombieComponent> ent, ref ComponentStartup args)
    {
        if (HasComp<PacifiedComponent>(ent) && !_statusEffects.HasStatusEffect(ent, "Pacified"))
            EnsureComp<ADTZombifiedPacifistComponent>(ent);
    }

    private void OnCloning(Entity<HumanoidProfileComponent> ent, ref CloningEvent args)
    {
        if (HasComp<ZombieComponent>(ent))
            RestoreZombieAlteredComponents(args.CloneUid);

        if (HasComp<ADTZombifiedPacifistComponent>(ent) && args.Settings.Components.Contains("Pacified"))
            EnsureComp<PacifiedComponent>(args.CloneUid);

        if (!TryComp<HumanoidProfileComponent>(args.CloneUid, out var cloneProfile))
            return;

        var clone = new Entity<HumanoidProfileComponent?>(args.CloneUid, cloneProfile);

        _humanoidProfile.SetSex(clone, ent.Comp.Sex);
        _humanoidProfile.SetGender(clone, ent.Comp.Gender);
        _humanoidProfile.SetAge(clone, ent.Comp.Age);

        if (TryComp<GrammarComponent>(args.CloneUid, out var grammar))
            _grammar.SetGender((args.CloneUid, grammar), ent.Comp.Gender);

        _identity.QueueIdentityUpdate(args.CloneUid);
    }

    private void RestoreZombieAlteredComponents(EntityUid clone)
    {
        if (MetaData(clone).EntityPrototype is not { } proto)
            return;

        var registry = new ComponentRegistry();
        foreach (var name in ZombieAlteredComponents)
        {
            if (proto.Components.TryGetValue(name, out var entry))
                registry.Add(name, entry);
        }

        EntityManager.AddComponents(clone, registry);

        foreach (var organ in _body.EnumerateOrgans<HandOrganComponent>(clone))
        {
            _hands.AddHand(clone, organ.Comp2.HandID, organ.Comp2.Data);
        }

        if (proto.TryGetComponent<DamageableComponent>(out var damageable, _compFactory))
            _damageable.SetDamageModifierSetId(clone, damageable.DamageModifierSetId);
    }
}
