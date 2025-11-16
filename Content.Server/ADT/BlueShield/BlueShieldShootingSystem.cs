using Content.Server.AlertLevel;
using Content.Shared.ADT.CantShoot;
using Content.Shared.Weapons.Reflect;

namespace Content.Server.ADT.BlueShield;

public sealed class BlueShieldShootingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AlertLevelChangedEvent>(BSOCanShoot);
    }
    private void BSOCanShoot(AlertLevelChangedEvent args)
    {
        if (args.AlertLevel == "gamma" || args.AlertLevel == "epsilon" || args.AlertLevel == "delta")
        {
            // Находим все сущности с CantShootComponent
            var query = EntityQueryEnumerator<CantShootComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                // Делаем так, чтобы спящие карпы не подвергались этой системе
                if (TryComp<ReflectComponent>(uid, out var comp) && comp.ReflectProb == 1)
                    continue;
                // Удаляем с сущности CantShootComponent
                RemComp<CantShootComponent>(uid);
            }
        }
    }
}
