using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing;

public abstract class SharedJumpbootsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpbootsComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<JumpbootsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, JumpbootsComponent component, MapInitEvent args)
    {
        component.ActionEntity ??= FindStoredAction(uid, component.Action);

        _actionContainer.AddAction(uid, ref component.ActionEntity, component.Action);
        Dirty(uid, component);
    }

    private EntityUid? FindStoredAction(EntityUid uid, EntProtoId action)
    {
        if (!TryComp<ActionsContainerComponent>(uid, out var container))
            return null;

        foreach (var stored in container.Container.ContainedEntities)
        {
            if (MetaData(stored).EntityPrototype?.ID == action.Id)
                return stored;
        }

        return null;
    }

    private void OnGetActions(EntityUid uid, JumpbootsComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ActionEntity, component.Action);
    }
}

[DataDefinition]
public sealed partial class JumpbootsActionEvent : WorldTargetActionEvent
{
    [DataField]
    public SoundSpecifier? Sound { get; private set; } = new SoundPathSpecifier("/Audio/Effects/Footsteps/suitstep2.ogg");
}
