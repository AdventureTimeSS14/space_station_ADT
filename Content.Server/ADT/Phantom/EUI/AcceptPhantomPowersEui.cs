using Content.Server.EUI;
using Content.Shared.Phantom;
using Content.Shared.Eui;
using Content.Shared.Phantom.Components;
using Content.Server.Phantom.EntitySystems;

namespace Content.Server.Phantom;

public sealed class AcceptPhantomPowersEui : BaseEui
{
    private readonly PhantomSystem _phantom;
    private readonly PhantomComponent _component;
    private readonly EntityUid _uid;

    public AcceptPhantomPowersEui(EntityUid uid, PhantomSystem phantom, PhantomComponent comp)
    {
        _uid = uid;
        _phantom = phantom;
        _component = comp;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptPhantomPowersChoiceMessage choice ||
            choice.Button == AcceptPhantomPowersButton.Deny)
        {
            Close();
            return;
        }

        _phantom.MakePuppet(_uid, _component);
        Close();
    }
}
