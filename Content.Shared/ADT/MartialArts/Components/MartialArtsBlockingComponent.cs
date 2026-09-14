using Robust.Shared.GameStates;

namespace Content.Shared.ADT.MartialArts;

/// <summary>
/// Marker component for clothing. While worn in a slot checked by
/// <see cref="SharedMartialArtsSystem"/>, prevents the wearer from using martial arts combos.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MartialArtsBlockingComponent : Component;
