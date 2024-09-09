using Content.Server.Chat.Systems;
using Content.Shared.ADT.TimeDespawnDamage;
using Robust.Shared.Timing;
using Robust.Shared.Random;
using Content.Shared.Damage;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;


public sealed class TimeDespawnDamageSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimeDespawnDamageComponent, DamageChangedEvent>(OnMobStateDamage);
    }

    private void OnMobStateDamage(EntityUid uid, TimeDespawnDamageComponent component, DamageChangedEvent args)
    {
        if (EntityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
        {
            var damagePerGroup = damageable.Damage.GetDamagePerGroup(_prototypeManager);
            if (damagePerGroup.TryGetValue("ADTTime", out var timeDamage))
            {
                Log.Debug($"Сущности {ToPrettyString(uid)} нанесли: '{timeDamage}' временного дамага.");
                // if (timeDamage > 70)
                // {
                //     if (TryComp<BloodCoughComponent>(uid, out var posting))
                //     {
                //         posting.CheckCoughBlood = true;
                //     }
                // }
                // if (timeDamage <= 70)
                // {
                //     if (TryComp<BloodCoughComponent>(uid, out var posting))
                //     {
                //         posting.CheckCoughBlood = false;
                //     }
                // }
            }
        }
        else
        {
            Log.Debug($"Сущность {ToPrettyString(uid)} не имеет компонента DamageableComponent.");
        }
    }
}
