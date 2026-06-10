using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Weapons.Melee.Components;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem : EntitySystem
{
    public override void Initialize()
    {
    	SubscribeLocalEvent<MegafaunaComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);
        InitializeDrake();
    }

    private void OnAttemptMeleeThrowOnHit(Entity<MegafaunaComponent> _, ref AttemptMeleeThrowOnHitEvent args)
    {
    	args.Cancelled = true;
    }
}
