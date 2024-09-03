using Robust.Shared.GameStates;
using Content.Shared.Damage;

namespace Content.Shared.ADT.Damage.Components;

/// <summary>
/// This is used for an effect that nullifies <see cref="SlowOnDamageComponent"/> and adds an alert.
/// Thanks EmoGarbage404 for contributing this mechanic.
/// https://github.com/space-wizards/space-station-14/pull/31322
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SlowOnDamageSystem))]
public sealed partial class IgnoreSlowOnDamageComponent : Component;