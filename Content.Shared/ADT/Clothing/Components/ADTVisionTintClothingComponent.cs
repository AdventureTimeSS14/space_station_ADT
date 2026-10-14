using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Clothing;

/// <summary>
/// Одежда, которая красит обзор носителя, пока включена.
/// Требует <c>ItemToggle</c> — он даёт переключение по ПКМ и хранит состояние.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTVisionTintClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;

    /// <summary>
    /// Сила окраски, 0-1.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Amount = 0.3f;
}
