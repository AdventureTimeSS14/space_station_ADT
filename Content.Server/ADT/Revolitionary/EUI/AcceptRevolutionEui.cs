using Content.Server.EUI;
using Content.Shared.Revolutionary;
using Content.Shared.Eui;
using Content.Shared.Bible.Components;
using Content.Server.Bible;
using Content.Shared.Revolutionary.Components;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Revolutionary;

public sealed class AcceptRevolutionEui : BaseEui
{
    private readonly EntityUid _uid;
    private readonly EntityUid _target;
    private readonly HeadRevolutionaryComponent _comp;
    private readonly RevolutionaryRuleSystem _headrev;

    public AcceptRevolutionEui(EntityUid uid, EntityUid target, HeadRevolutionaryComponent comp, RevolutionaryRuleSystem headrev)
    {
        _uid = uid;
        _target = target;
        _comp = comp;
        _headrev = headrev;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not AcceptRevolutionChoiceMessage choice ||
            choice.Button == AcceptRevolutionButton.Deny)
        {
            Close();
            return;
        }

        _headrev.MakeEntRev(_uid, _target, _comp);
        Close();
    }
}
