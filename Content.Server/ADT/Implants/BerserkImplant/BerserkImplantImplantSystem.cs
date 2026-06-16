using Content.Shared.ADT.Implants.BerserkImplant;
using Content.Shared.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Server.ADT.Implants.BerserkImplant;

public sealed class BerserkImplantImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BerserkImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<BerserkImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanted(EntityUid uid, BerserkImplantComponent comp, ref ImplantImplantedEvent args)
    {
        var owner = args.Implanted;

        if (!TryComp<BerserkImplantUserComponent>(owner, out var user))
            user = AddComp<BerserkImplantUserComponent>(owner);

        user.ActionUid = _actions.AddAction(owner, user.ActionProto);
    }

    private void OnRemoved(EntityUid uid, BerserkImplantComponent comp, ref ImplantRemovedEvent args)
    {
        var owner = args.Implanted;

        if (!TryComp<BerserkImplantUserComponent>(owner, out var user))
            return;

        _actions.RemoveAction(owner, user.ActionUid);
        RemComp<BerserkImplantUserComponent>(owner);
        RemComp<BerserkImplantActiveComponent>(owner);
    }
}
