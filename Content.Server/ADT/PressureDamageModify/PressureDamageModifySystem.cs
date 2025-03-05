using Content.Server.Popups;
using Content.Shared.Projectiles;
using Content.Shared.StatusEffect;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;

namespace Content.Server.ADT.PressureDamageModify;

public sealed partial class PressureDamageModifySystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!; // ADT-Changeling-Tweak
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PressureDamageModifyComponent, ProjectileHitEvent>(OnProjectileHit);

        SubscribeLocalEvent<PressureDamageModifyComponent, MeleeHitEvent>(OnMeleeHit);
    }
    private void OnProjectileHit(EntityUid uid, PressureDamageModifyComponent component, ref ProjectileHitEvent args)
    {
        var pressure = 1f;

        if (_atmosphereSystem.GetContainingMixture(uid) is {} mixture)
        {
            pressure = MathF.Max(mixture.Pressure, 1f);
        }
        if (pressure >= component.Pressure)
        {
            args.Damage *= component.ProjDamage;
        }
    }

    private void OnMeleeHit(EntityUid uid, PressureDamageModifyComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit ||
            !args.HitEntities.Any() ||
            component.AdditionalDamage == null)
        {
            return;
        }

        foreach (var ent in args.HitEntities)
        {
            var pressure = 1f;

            if (_atmosphereSystem.GetContainingMixture(uid) is {} mixture)
            {
                pressure = MathF.Max(mixture.Pressure, 1f);
            }
            if (pressure <= component.Pressure)
            {
                _damage.TryChangeDamage(ent, component.AdditionalDamage);
            }
        }
    }
}
