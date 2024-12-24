using Content.Server._TornadoTech.FriendlyFire.Actions;
using Content.Server.Actions;
using Content.Shared.Toggleable;

namespace Content.Server._TornadoTech.FriendlyFire;

public sealed partial class FriendlyFireSystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;

    private void ToggleableInitialize()
    {
        SubscribeLocalEvent<FriendlyFireToggleableComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FriendlyFireToggleableComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<FriendlyFireToggleableComponent, ToggleActionEvent>(OnToggleAction);
    }

    private void OnStartup(Entity<FriendlyFireToggleableComponent> toggleable, ref ComponentStartup args)
    {
        toggleable.Comp.Action = _actions.AddAction(toggleable, toggleable.Comp.Prototype);
        _actions.SetToggled(toggleable.Comp.Action, GetState(toggleable));
    }

    private void OnRemove(Entity<FriendlyFireToggleableComponent> toggleable, ref ComponentRemove args)
    {
        if (toggleable.Comp.Action is not { } action)
            return;

        _actions.RemoveAction(toggleable, action);
    }

    private void OnToggleAction(Entity<FriendlyFireToggleableComponent> toggleable, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        Toggle(toggleable);
        _actions.SetToggled(toggleable.Comp.Action, GetState(toggleable));

        args.Handled = true;
    }
}
