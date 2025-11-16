using Content.Server.ADT.BlueShield.Components;
using Content.Server.AlertLevel;
using Content.Shared.ADT.CantShoot;

namespace Content.Server.ADT.BlueShield.Systems;

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
                // Если находим офицера синего щита, то убираем CantShootComponent
                if (HasComp<BlueShieldComponent>(uid))
                {
                    RemComp<CantShootComponent>(uid);
                }
            }
        }
    }
}
