using Robust.Shared.GameStates;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingHeadslugContainerComponent : Component
{
    [NotNull]
    public Container? Container;
}
