// Inspired by Nyanotrasen
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Interaction;
using Content.Shared.CharecterFlavor;
using Content.Shared.ADT.CharecterFlavor;

namespace Content.Server.CharecterFlavor;

public sealed class CharecterFlavorSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharecterFlavorComponent, ActivateInWorldEvent>(OnOpenUi);

        Subs.BuiEvents<CharecterFlavorComponent>(CharecterFlavorUiKey.Key, subs =>
        {
            subs.Event<OpenURLMessage>(OnActivate);
        });
    }

    private void OnOpenUi(EntityUid uid, CharecterFlavorComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        Dirty(uid, component);
        _uiSystem.TryOpenUi(uid, CharecterFlavorUiKey.Key, actor.Owner);
    }

    private void OnActivate(Entity<CharecterFlavorComponent> ent, ref OpenURLMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;
    }
}
