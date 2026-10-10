//

using Content.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed class EldritchIdCardSystem : SharedEldritchIdCardSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EldritchIdCardComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<EldritchIdCardComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Comp.PortalOne))
            QueueDel(ent.Comp.PortalOne);

        if (!TerminatingOrDeleted(ent.Comp.PortalTwo))
            QueueDel(ent.Comp.PortalTwo);
    }
}
