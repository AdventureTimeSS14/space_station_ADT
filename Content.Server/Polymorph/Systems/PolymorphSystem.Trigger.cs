using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.Mech.Components;

namespace Content.Server.Polymorph.Systems;

public sealed partial class PolymorphSystem
{
    /// <summary>
    /// Need to do this so we don't get a collection enumeration error in physics by polymorphing
    /// an entity we're colliding with in case of TriggerOnCollide.
    /// Also makes sure other trigger effects don't activate in nullspace after we have polymorphed.
    /// </summary>
    private Queue<(EntityUid Ent, ProtoId<PolymorphPrototype> Polymorph)> _queuedPolymorphUpdates = new();

    private void InitializeTrigger()
    {
        SubscribeLocalEvent<PolymorphOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<PolymorphOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.User == null)
            return;

        // Ganimed edit start
        var user = args.User.Value;

        if (TryComp<MechPilotComponent>(user, out var pilot) && pilot.Mech != null)
            return;

        _queuedPolymorphUpdates.Enqueue((user, ent.Comp.Polymorph));
        args.Handled = true;
        // Ganimed edit end
    }

    public void UpdateTrigger()
    {
        while (_queuedPolymorphUpdates.TryDequeue(out var data))
        {
            if (TerminatingOrDeleted(data.Item1))
                continue;

            PolymorphEntity(data.Item1, data.Item2);
        }
    }
}
