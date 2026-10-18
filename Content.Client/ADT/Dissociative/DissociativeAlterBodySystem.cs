using Content.Client.Clickable;
using Content.Client.Interactable.Components;
using Content.Shared.ADT.Dissociative;
using Content.Shared.Examine;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Client.ADT.Dissociative;

public sealed class DissociativeAlterBodySystem : EntitySystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;

    private const string InputContext = "human";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DissociativeAlterBodyComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<DissociativeAlterBodyComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<InputComponent>(ent).ContextName = InputContext;
        EnsureComp<ClickableComponent>(ent);
        EnsureComp<InteractionOutlineComponent>(ent);
        EnsureComp<ExaminerComponent>(ent);

        if (_player.LocalEntity == ent.Owner && _input.Contexts.Exists(InputContext))
            _input.Contexts.SetActiveContext(InputContext);
    }
}
