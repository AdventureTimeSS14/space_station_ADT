namespace Content.Shared.ADT.Novakid;

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
