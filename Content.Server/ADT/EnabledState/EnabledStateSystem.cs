using Content.Shared.ADT.HandleItemStateVisual;
using Content.Shared.Interaction;

namespace Content.Server.ADT.EnabledState;

public sealed class EnabledStateSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem _appearanceSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<EnabledStateComponent, ActivateInWorldEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<EnabledStateComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!Resolve(ent, ref ent.Comp!))
            return;

        if (args.Handled)
            return;

        var oldState = ent.Comp.Enabled;

        ent.Comp.Enabled = !oldState;

        if (TryComp(ent, out AppearanceComponent? appearance))
        {
            var state = ent.Comp.Enabled ? HandleEnabledItemStateVisual.On : HandleEnabledItemStateVisual.Off;
            _appearanceSystem.SetData(ent, HandleEnabledItemStateVisual.Visual, state, appearance);
        }

        args.Handled = true;
    }
}
