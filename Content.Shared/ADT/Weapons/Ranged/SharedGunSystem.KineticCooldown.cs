using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    public void ADTSetNextFire(Entity<GunComponent?> ent, TimeSpan nextFire)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.NextFire == nextFire)
            return;

        ent.Comp.NextFire = nextFire;
        DirtyField(ent, nameof(GunComponent.NextFire));
    }
}
