using Content.Client.Actions;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Actions;

public sealed class ADTTargetActionIconSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TargetActionComponent, AppearanceChangeEvent>(OnAppearanceChange,
            after: new[] { typeof(GenericVisualizerSystem), typeof(ActionIconVisualsSystem) });
    }

    private void OnAppearanceChange(Entity<TargetActionComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (_ui.GetUIController<ActionUIController>().SelectingTargetFor != ent.Owner)
            return;

        if (!_sprite.LayerExists((ent.Owner, sprite), ActionVisuals.IconToggled))
            return;

        _sprite.LayerSetVisible((ent.Owner, sprite), ActionVisuals.Icon, false);
        _sprite.LayerSetVisible((ent.Owner, sprite), ActionVisuals.IconToggled, true);
    }

    public void ResetIcon(EntityUid? uid)
    {
        if (uid is not { } action)
            return;

        if (!HasComp<TargetActionComponent>(action) || !TryComp<SpriteComponent>(action, out var sprite))
            return;

        if (!_sprite.LayerExists((action, sprite), ActionVisuals.IconToggled))
            return;

        if (TryComp<ActionComponent>(action, out var comp) && comp.Toggled)
            return;

        _sprite.LayerSetVisible((action, sprite), ActionVisuals.Icon, true);
        _sprite.LayerSetVisible((action, sprite), ActionVisuals.IconToggled, false);
    }
}
