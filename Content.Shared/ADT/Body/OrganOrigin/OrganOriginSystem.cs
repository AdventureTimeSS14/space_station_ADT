using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;

namespace Content.Shared.ADT.Body.OrganOrigin;

public sealed class OrganOriginSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;

    private static readonly DamageSpecifier RejectionDamage = new()
    {
        DamageDict = { ["Poison"] = 30 },
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetachableOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnGotRemoved(Entity<DetachableOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (TerminatingOrDeleted(ent) || !TryComp<HumanoidProfileComponent>(args.Target, out var profile))
            return;

        var origin = EnsureComp<OrganOriginComponent>(ent);
        origin.Species ??= profile.Species;
    }

    public void OnTransplanted(EntityUid limb, EntityUid body)
    {
        if (!TryComp<OrganOriginComponent>(limb, out var origin) || origin.Species is not { } species)
            return;

        if (!TryComp<HumanoidProfileComponent>(body, out var profile) || profile.Species == species)
            return;

        origin.Species = profile.Species;
        Dirty(limb, origin);

        _damageable.TryChangeDamage(body, RejectionDamage, ignoreResistances: true);
    }
}
