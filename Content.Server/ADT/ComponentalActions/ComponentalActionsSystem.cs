using Content.Shared.Actions;
using Content.Shared.ADT.ComponentalActions;
using Content.Shared.ADT.ComponentalActions.Components;

namespace Content.Server.ADT.ComponentalActions;

public sealed partial class ComponentalActionsSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAction<TeleportActComponent>();
        SubscribeAction<ProjectileActComponent>();
        SubscribeAction<HealActComponent>();
        SubscribeAction<JumpActComponent>();
        SubscribeAction<StasisHealActComponent>();
        SubscribeAction<InvisibilityActComponent>();
        SubscribeAction<ElectrionPulseActComponent>();

        InitializeAbilities();
    }

    private void SubscribeAction<T>() where T : Component, IComponentalActionComponent
    {
        SubscribeLocalEvent<T, MapInitEvent>(OnActionMapInit<T>);
        SubscribeLocalEvent<T, ComponentShutdown>(OnActionShutdown<T>);
    }

    private void OnActionMapInit<T>(Entity<T> ent, ref MapInitEvent args) where T : Component, IComponentalActionComponent
    {
        var actionEntity = ent.Comp.ActionEntity;
        _actions.AddAction(ent.Owner, ref actionEntity, ent.Comp.Action);
        ent.Comp.ActionEntity = actionEntity;
    }

    private void OnActionShutdown<T>(Entity<T> ent, ref ComponentShutdown args) where T : Component, IComponentalActionComponent
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }
}
