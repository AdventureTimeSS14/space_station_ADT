<<<<<<< HEAD
using Content.Shared.Body.Systems;
=======
using Content.Shared.Gibbing;
>>>>>>> upstreamwiz/master
using Content.Shared.Inventory;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class GibOnTriggerSystem : XOnTriggerSystem<GibOnTriggerComponent>
{
<<<<<<< HEAD
    [Dependency] private readonly SharedBodySystem _body = default!;
=======
    [Dependency] private readonly GibbingSystem _gibbing = default!;
>>>>>>> upstreamwiz/master
    [Dependency] private readonly InventorySystem _inventory = default!;

    protected override void OnTrigger(Entity<GibOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        if (ent.Comp.DeleteItems)
        {
            var items = _inventory.GetHandOrInventoryEntities(target);
            foreach (var item in items)
            {
                PredictedQueueDel(item);
            }
        }

<<<<<<< HEAD
        _body.GibBody(target, true);
=======
        _gibbing.Gib(target, user: args.User);
>>>>>>> upstreamwiz/master
        args.Handled = true;
    }
}
