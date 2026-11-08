namespace Content.Shared.Clothing;

public sealed partial class ClothingSpeedModifierSystem
{
    public void SetWalkSpeedModifier(ClothingSpeedModifierComponent component, float value)
    {
        component.WalkModifier = value;
    }

    public void SetSprintSpeedModifier(ClothingSpeedModifierComponent component, float value)
    {
        component.SprintModifier = value;
    }
}