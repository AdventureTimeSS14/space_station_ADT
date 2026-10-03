//

using Content.Shared.Actions;
using Content.Shared.Heretic.Components.PathSpecific.Ash;
using Content.Shared.Toggleable;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Ash;

public sealed partial class ToggleActionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleActionComponent, ToggleActionEvent>(OnToggle);
    }

    private void OnToggle(Entity<ToggleActionComponent> ent, ref ToggleActionEvent args)
    {
        _actions.SetToggled(args.Action.AsNullable(), !args.Action.Comp.Toggled);
    }
}
