// Simple Station

using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Traits
{
    /// <summary>
    ///     Entity cannot see color.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MonochromacyComponent : Component
    {

    }
}
