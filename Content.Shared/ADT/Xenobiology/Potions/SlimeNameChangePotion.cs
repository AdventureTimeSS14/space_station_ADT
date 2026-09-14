using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that renames a sentient creature. The new name is set through a UI.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeNameChangePotionComponent : Component
{
    [DataField("assignedName"), AutoNetworkedField]
    public string AssignedName = string.Empty;
}

public sealed partial class SlimeNameChangePotionSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeNameChangePotionComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SlimeNameChangePotionComponent, SlimeNameChangePotionNewNameChangedMessage>(OnNewNameChanged);
    }

    private void OnAfterInteract(Entity<SlimeNameChangePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!HasComp<MindContainerComponent>(target))
            return;

        if (string.IsNullOrWhiteSpace(ent.Comp.AssignedName))
        {
            _popup.PopupPredicted(Loc.GetString("xeno-potion-name-not-set"), args.User, args.User);
            return;
        }

        args.Handled = true;

        var oldName = Name(target);
        _metaData.SetEntityName(target, ent.Comp.AssignedName);

        if (args.User != target)
            _popup.PopupPredicted(Loc.GetString("xeno-potion-name-renamed", ("old", oldName), ("new", Name(target))), args.User, args.User);
        _popup.PopupPredicted(Loc.GetString("xeno-potion-name-you-are", ("name", Name(target))), target, target);

        PredictedQueueDel(args.Used);
    }

    private void OnNewNameChanged(Entity<SlimeNameChangePotionComponent> ent, ref SlimeNameChangePotionNewNameChangedMessage args)
    {
        ent.Comp.AssignedName = args.NewName;
        Dirty(ent);
    }
}

[Serializable, NetSerializable]
public enum SlimeNameChangePotionUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SlimeNameChangePotionNewNameChangedMessage(string newName) : BoundUserInterfaceMessage
{
    public string NewName { get; } = newName;
}
