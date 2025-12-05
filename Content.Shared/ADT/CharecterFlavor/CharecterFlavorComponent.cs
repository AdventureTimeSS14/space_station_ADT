using Robust.Shared.GameStates;

namespace Content.Shared.ADT.CharecterFlavor;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class CharecterFlavorComponent : Component
{
    /// <summary>
    /// основной текст флавора, описание персонажа
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string FlavorText = string.Empty;
    /// <summary>
    /// ООС заметки
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string OOCNotes = string.Empty;
    /// <summary>
    /// ссылка на изображение, что будет использоваться в качестве хэдшота
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string HeadshotUrl = "https://i.pinimg.com/736x/0e/04/5a/0e045a8c7792396c13ec332817b7f4be.jpg";
    /// <summary>
    /// используется для передачи на клиент битов картинки
    /// можно сделать лучше конечно, но слишком запарно
    /// </summary>

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public byte[] HeadshotBytes;
}
