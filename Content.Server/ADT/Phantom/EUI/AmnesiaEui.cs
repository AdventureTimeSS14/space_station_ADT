using Content.Server.EUI;
using Content.Shared.Phantom;
using Content.Shared.Eui;
using Content.Shared.Phantom.Components;
using Content.Server.Phantom.EntitySystems;

namespace Content.Server.Phantom;

public sealed class PhantomAmnesiaEui : BaseEui
{

    public PhantomAmnesiaEui()
    {
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        Close();
    }
}
