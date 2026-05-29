using Content.Shared.Mobs;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Silicon;

public sealed class MobIpcSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobIpcComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<MobIpcComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead &&
            ent.Comp.DisablePointLightOnDeath &&
            _pointLight.TryGetLight(ent.Owner, out var lightComp) &&
            lightComp.Enabled)
        {
            _pointLight.SetEnabled(ent.Owner, false);
            ent.Comp.LightDisabledByDeath = true;
            Dirty(ent.Owner, ent.Comp);
            return;
        }

        if (args.OldMobState == MobState.Dead &&
            args.NewMobState != MobState.Dead &&
            ent.Comp.LightDisabledByDeath)
        {
            _pointLight.SetEnabled(ent.Owner, true);
            ent.Comp.LightDisabledByDeath = false;
            Dirty(ent.Owner, ent.Comp);
        }
    }
}
