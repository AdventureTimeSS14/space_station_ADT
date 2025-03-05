using Content.Shared.Interaction.Events;

namespace Content.Server.ADT.AddComponentsOnUse;

public sealed class AddComponentsOnUseSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddComponentsOnUseComponent, UseInHandEvent>(OnUsed);
    }

    private void OnUsed(EntityUid uid, AddComponentsOnUseComponent component, UseInHandEvent args)
    {
        EntityManager.AddComponents(args.User, component.Components);
        if (component.DeleteOnUse)
            QueueDel(uid);
    }
}
