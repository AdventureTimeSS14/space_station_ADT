<<<<<<< HEAD
=======
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.GameStates;

>>>>>>> wizards-filtered
namespace Content.Shared.Speech.Components;

/// <summary>
/// Marks a speech status effect that transforms spoken text to uppercase.
/// </summary>
<<<<<<< HEAD
[RegisterComponent]
public sealed partial class AllCapsAccentComponent : Component;
=======
[RegisterComponent, NetworkedComponent]
[Access(typeof(AllCapsAccentSystem))]
public sealed partial class AllCapsAccentComponent : BaseAccentComponent;
>>>>>>> wizards-filtered
