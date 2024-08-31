using Content.Server.EUI;
using Content.Shared.ADT.Phantom;
using Content.Shared.Eui;
using Content.Shared.ADT.Phantom.Components;
using Content.Server.ADT.Phantom.EntitySystems;

namespace Content.Server.ADT.Phantom;

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
