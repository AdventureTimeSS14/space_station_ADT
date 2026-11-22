using Content.Client.ADT.Mech.UI;
using Content.Client.Items;
using Content.Shared.ADT.Mech.Components;
using Content.Shared.ADT.Mech.EntitySystems;

namespace Content.Client.ADT.Mech.Systems;

public sealed class MechMagazineStatusSystem : EntitySystem
{
    [Dependency] private MechGunReloadSystem _reload = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<MechMagazineComponent>(ent => new MechMagazineStatusControl(ent, _reload));
    }
}
