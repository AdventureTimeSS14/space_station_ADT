using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Clothing;

/// <summary>
/// Одежда с этим компонентом красит обзор носителя, пока надета.
/// Вместе с <c>ItemToggle</c> окраску можно выключить через ПКМ.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ADTVisionTintComponent : Component
{
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Сила окраски, 0-1.
    /// </summary>
    [DataField]
    public float Amount = 0.3f;
}
