using Content.Shared.Actions;
using Content.Server.ADT.Administration;

namespace Content.Server.ADT.Actions.Slimecats;

public sealed partial class AdminGodSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdminGodComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, AdminGodComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.ActionForAdminGod1);
        _actions.AddAction(uid, ref component.ActionEntity, component.ActionForAdminGod2);
        _actions.AddAction(uid, ref component.ActionEntity, component.ActionForAdminGod3);
    }
}