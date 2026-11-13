using Content.Shared.ADT.Surgery.Components;
using Content.Shared.Standing;
using Content.Shared.Wires;

namespace Content.Shared.ADT.Surgery;

public sealed class SurgeryPanelLockSystem : EntitySystem
{
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OperatedComponent, AttemptChangePanelEvent>(OnAttemptChangePanel);
    }

    private void OnAttemptChangePanel(Entity<OperatedComponent> ent, ref AttemptChangePanelEvent args)
    {
        if (ent.Comp.GraphId != null && _standing.IsDown(ent.Owner))
            args.Cancelled = true;
    }
}
