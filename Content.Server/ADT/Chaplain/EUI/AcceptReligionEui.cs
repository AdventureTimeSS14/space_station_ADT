using Content.Server.ADT.Chaplain;
using Content.Server.EUI;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Chaplain;
using Content.Shared.Eui;

namespace Content.Server.Chaplain;

public sealed class AcceptReligionEui : BaseEui
{
    private readonly Entity<ChaplainComponent> _chaplain;
    private readonly EntityUid _target;
    private readonly ChaplainSystem _system;

    public AcceptReligionEui(Entity<ChaplainComponent> chaplain, EntityUid target, ChaplainSystem system)
    {
        _chaplain = chaplain;
        _target = target;
        _system = system;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is AcceptReligionChoiceMessage { Button: AcceptReligionButton.Accept })
            _system.MakeFollower(_chaplain, _target);

        Close();
    }
}
