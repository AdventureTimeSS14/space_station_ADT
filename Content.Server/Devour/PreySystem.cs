using Content.Shared.Actions;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;

namespace Content.Server.Devour;

/// <summary>
/// Gives prey consent toggle action on spawn.
/// </summary>
public sealed class PreySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PreyComponent, MapInitEvent>(OnPreyInit);
    }

    private void OnPreyInit(EntityUid uid, PreyComponent comp, MapInitEvent args)
    {
        _actions.AddAction(uid, "ActionToggleUnwillingConsent");
    }
}


