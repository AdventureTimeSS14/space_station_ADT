using Content.Server.EUI;
using Content.Shared.ADT.Phantom;
using Content.Shared.Eui;
using Content.Shared.ADT.Phantom.Components;
using Content.Server.ADT.Phantom.EntitySystems;

namespace Content.Server.ADT.Phantom;

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

