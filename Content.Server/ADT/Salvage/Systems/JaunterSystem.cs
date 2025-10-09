using Content.Server.ADT.Salvage.Components;
using Content.Shared.Interaction.Events;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class JaunterSystem : EntitySystem
{
    [Dependency] private readonly JaunterPortalSystem _portal = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JaunterComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(EntityUid uid, JaunterComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        _portal.SpawnLinkedPortal(args.User);
        QueueDel(uid);
        args.Handled = true;
    }
}
