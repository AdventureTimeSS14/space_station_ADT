using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing;

public abstract class SharedJumpbootsSystem : EntitySystem
{
    [Dependency] private readonly ClothingSpeedModifierSystem _clothingSpeedModifier = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedActionsSystem _sharedActions = default!;
    [Dependency] private readonly SharedActionsSystem _actionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpbootsComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<JumpbootsComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, JumpbootsComponent component, MapInitEvent args)
    {
        _actionContainer.AddAction(uid, ref component.ActionEntity, component.Action);
        Dirty(uid, component);
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
