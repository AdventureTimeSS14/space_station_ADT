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
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeNameChangePotionComponent, SlimeNameChangePotionNewNameChangedMessage>(OnNewNameChanged);
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
