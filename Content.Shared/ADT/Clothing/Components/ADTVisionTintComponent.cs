using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Clothing;

/// <summary>
/// Красит обзор носителя. Выдаётся одеждой через ClothingGrantComponent.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTVisionTintComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    [DataField, AutoNetworkedField]
    public float Amount = 0.3f;
}
