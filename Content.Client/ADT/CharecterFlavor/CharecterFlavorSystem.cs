using Content.Shared.CharecterFlavor;
using Robust.Client.UserInterface;

namespace Content.Client.CharecterFlavor;

public sealed class CharecterFlavorSystem : EntitySystem
{ 
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<OpenURLEvent>(OnOpenURL);
    }

    private void OnOpenURL(OpenURLEvent args)
    {
        var uriOpener = IoCManager.Resolve<IUriOpener>();
        uriOpener.OpenUri(args.URL);
    }
}
