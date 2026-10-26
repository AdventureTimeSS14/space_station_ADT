using Content.Shared.Clothing.Components;

namespace Content.Shared.Clothing.EntitySystems;

public sealed partial class FireProtectionSystem
{
    public void SetFireProtection(FireProtectionComponent component, float reduction)
    {
        component.Reduction = reduction;
    }
}