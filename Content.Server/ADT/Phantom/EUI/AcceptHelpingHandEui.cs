using Content.Server.EUI;
using Content.Shared.ADT.Phantom;
using Content.Shared.Eui;
using Content.Shared.ADT.Phantom.Components;
using Content.Server.ADT.Phantom.EntitySystems;

namespace Content.Server.ADT.Phantom;

public sealed class AcceptHelpingHandEui : BaseEui
{
    private readonly PhantomSystem _phantom;
    private readonly PhantomComponent _component;
    private readonly EntityUid _uid;

    public AcceptHelpingHandEui(EntityUid uid, PhantomSystem phantom, PhantomComponent comp)
    {
        _uid = uid;
        _phantom = phantom;
        _component = comp;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptHelpingHandChoiceMessage choice ||
            choice.Button == AcceptHelpingHandButton.Deny)
        {
            Close();
            return;
        }

        _phantom.OnHelpingHandAccept(_uid, _component);
        Close();
    }
}
