using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Construction
{
    /// <summary>
    /// Это исправит прикручивание объектов на лужи. Оффы так насрали, что <see cref="PhysicsComponent"/> теперь не читается с прототипа, либо читается через жопу.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class NeverPreventAnchorComponent : Component
    {
    }
}
