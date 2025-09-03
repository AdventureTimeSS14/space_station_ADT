using Content.Shared.Actions;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;

namespace Content.Server.Devour;

/// <summary>
/// Gives predator action on map init/startup.
/// </summary>
public sealed class PredatorSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PredatorComponent, MapInitEvent>(OnPredInit);
    }

    private void OnPredInit(EntityUid uid, PredatorComponent comp, MapInitEvent args)
    {
        _actions.AddAction(uid, "ActionDevour");
    }
}


