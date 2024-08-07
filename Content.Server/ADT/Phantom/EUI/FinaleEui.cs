using Content.Server.EUI;
using Content.Shared.Phantom;
using Content.Shared.Eui;
using Content.Shared.Phantom.Components;
using Content.Server.Phantom.EntitySystems;

namespace Content.Server.Phantom;

public sealed class PhantomFinaleEui : BaseEui
{
    private readonly PhantomSystem _phantom;
    private readonly PhantomComponent _component;
    private readonly EntityUid _uid;
    private readonly PhantomFinaleType _type;

    public PhantomFinaleEui(EntityUid uid, PhantomSystem phantom, PhantomComponent comp, PhantomFinaleType type)
    {
        _uid = uid;
        _phantom = phantom;
        _component = comp;
        _type = type;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not PhantomFinaleChoiceMessage choice ||
            choice.Button == PhantomFinaleButton.Deny)
        {
            Close();
            return;
        }

        _phantom.Finale(_uid, _component, _type);

        Close();
    }
}

