namespace Content.Shared.ADT.NovakidFeatures;

[RegisterComponent]
public sealed partial class NovakidGlowingComponent : Component
{
    [DataField]
    public Color GlowingColor;

    NovakidGlowingComponent(Color color)
    {
        GlowingColor = color;
    }
}
