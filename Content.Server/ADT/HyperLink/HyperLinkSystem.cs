// Inspired by Nyanotrasen
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Interaction;
using Content.Shared.HyperLink;

namespace Content.Server.HyperLink;

public sealed class HyperLinkSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HyperLinkComponent, ActivateInWorldEvent>(OnOpenUi);

        Subs.BuiEvents<HyperLinkComponent>(HyperLinkUiKey.Key, subs =>
        {
            subs.Event<OpenURLMessage>(OnActivate);
        });
    }

    private void OnOpenUi(EntityUid uid, HyperLinkComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, HyperLinkUiKey.Key, actor.Owner);
    }

    private void OnActivate(Entity<HyperLinkComponent> ent, ref OpenURLMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        OpenURL(actor.PlayerSession, ent.Comp.URL);
    }

    public void OpenURL(ICommonSession session, string url)
    {
        var ev = new OpenURLEvent(url);
        RaiseNetworkEvent(ev, session.Channel);
    }
}
