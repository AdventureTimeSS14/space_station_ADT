using Content.Shared.Actions.Components;
using Content.Shared.Clothing.Components;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Containers;

public static class ADTInternalContainers
{
    public const string SolutionContainerPrefix = "solution@";

    public static bool IsInternal(BaseContainer container)
    {
        return IsInternal(container.ID);
    }

    public static bool IsInternal(string containerId)
    {
        return containerId == ActionsContainerComponent.ContainerId
               || containerId == ToggleableClothingComponent.DefaultClothingContainerId
               || containerId.StartsWith(SolutionContainerPrefix);
    }
}
