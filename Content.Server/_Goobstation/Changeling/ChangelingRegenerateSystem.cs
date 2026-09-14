using Content.Goobstation.Shared.Changeling.Components;
using Content.Goobstation.Shared.Changeling.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Polymorph;

namespace Content.Goobstation.Server.Changeling;

public sealed partial class ChangelingRegenerateSystem : SharedChangelingRegenerateSystem
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private EntityQuery<ChangelingIdentityComponent> _lingQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingRegenerateComponent, PolymorphedEvent>(OnPolymorphed);

        _lingQuery = GetEntityQuery<ChangelingIdentityComponent>();
    }

    private void OnPolymorphed(Entity<ChangelingRegenerateComponent> ent, ref PolymorphedEvent args)
    {
        if (_lingQuery.TryComp(ent, out var ling)
            && ling.IsInLastResort)
            return;

        _polymorph.CopyPolymorphComponent<ChangelingRegenerateComponent>(ent, args.NewEntity);
    }

    // ADT-Tweak
    protected override void RegenerateHeal(Entity<ChangelingRegenerateComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent, out var dmg)
            || !_proto.TryIndex<DamageGroupPrototype>("Brute", out var brute))
            return;

        var heal = new DamageSpecifier();
        foreach (var (type, amount) in dmg.Damage.DamageDict)
        {
            if (amount > 0 && brute.DamageTypes.Contains(type))
                heal.DamageDict[type] = -(amount / 2);
        }

        if (heal.DamageDict.Count > 0)
            _damageable.TryChangeDamage((ent.Owner, dmg), heal, true, false);
    }
}
