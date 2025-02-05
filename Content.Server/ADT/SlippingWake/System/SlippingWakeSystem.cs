using Content.Shared.Slippery;
using Robust.Shared.Physics;



/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/

namespace Content.Server.ADT.SlippingWake;

public sealed class SlippingWakeSystem : EntitySystem
{
    [Dependency] private readonly SlipperySystem _slipperySystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlippingWakeComponent, PhysicsWakeEvent>(MobWakeCheck);
    }

    public async void MobWakeCheck(EntityUid uid, SlippingWakeComponent comp, PhysicsWakeEvent args)
    {
        /*
        // Ручное увеличение скорости
        if (TryComp(args.Entity, out PhysicsComponent? physics))
        {
            Log.Warning($"[INFO] Before manual velocity: {physics.LinearVelocity}");

            if (physics.LinearVelocity.LengthSquared() > 0) // Проверяем, есть ли движение
            {
                var direction = Vector2.Normalize(physics.LinearVelocity);
                var addedVelocity = direction * comp.LaunchForwardsMultiplier; // Ускоряем в этом направлении
                _physics.SetLinearVelocity(args.Entity, physics.LinearVelocity + addedVelocity, body: physics);
                Log.Warning($"[INFO] Added velocity: {addedVelocity}");
            }

            Log.Warning($"[INFO] After manual velocity: {physics.LinearVelocity}");
        }
        */
        var hadSlipComponent = EnsureComp(args.Entity, out SlipperyComponent slipComponent);
        if (!hadSlipComponent)
        {
            slipComponent.SuperSlippery = true;
            slipComponent.ParalyzeTime = 5;
            slipComponent.LaunchForwardsMultiplier = comp.LaunchForwardsMultiplier;
        }


        _slipperySystem.TrySlip(args.Entity, slipComponent, args.Entity, requiresContact: false);


        if (!hadSlipComponent)
        {
            RemComp(args.Entity, slipComponent);
        }
    }

}

