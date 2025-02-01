using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;
using Content.Shared.ADT.ModSuits;
using Robust.Shared.Containers;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This component indicates that this clothing is attached to some other entity with a <see
///     cref="ToggleableClothingComponent"/>. When unequipped, this entity should be returned to the entity that it is
///     attached to, rather than being dumped on the floor or something like that. Intended for use with hardsuits and
///     hardsuit helmets.
/// </summary>
[Access(typeof(ToggleableClothingSystem), typeof(ModSuitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModAttachedClothingComponent : Component
{
    /// <summary>
    ///     The Id of the piece of clothing that this entity belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid AttachedUid;

    //ADT tweak start
    /// <summary>
    ///     Container ID for clothing that will be replaced with this one
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ClothingContainerId = DefaultClothingContainerId;

    [ViewVariables, NonSerialized]
    public ContainerSlot? ClothingContainer;

    public const string DefaultClothingContainerId = "replaced-clothing";
    //ADT tweak end
}
