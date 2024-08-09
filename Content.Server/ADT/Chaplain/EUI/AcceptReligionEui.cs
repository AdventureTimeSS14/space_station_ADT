using Content.Server.EUI;
using Content.Shared.Chaplain;
using Content.Shared.Eui;
using Content.Shared.Bible.Components;
using Content.Server.Bible;

namespace Content.Server.Chaplain;

public sealed class AcceptReligionEui : BaseEui
{
    private readonly EntityUid _uid;
    private readonly EntityUid _target;
    private readonly ChaplainComponent _comp;
    private readonly ChaplainSystem _chaplain;

    public AcceptReligionEui(EntityUid uid, EntityUid target, ChaplainComponent comp, ChaplainSystem chaplain)
    {
        _uid = uid;
        _target = target;
        _comp = comp;
        _chaplain = chaplain;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptReligionChoiceMessage choice ||
            choice.Button == AcceptReligionButton.Deny)
        {
            Close();
            return;
        }

        _chaplain.MakeBeliever(_uid, _target, _comp);
        Close();
    }
}
