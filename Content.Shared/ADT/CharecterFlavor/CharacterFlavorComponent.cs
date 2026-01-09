using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.CharecterFlavor;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent]
public sealed partial class CharacterFlavorComponent : Component
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
    public string HeadshotUrl = "https://media.discordapp.net/attachments/1446603657255850085";
}

[Serializable, NetSerializable]
public sealed partial class SetHeadshotUiMessage : EntityEventArgs
{
    public readonly NetEntity Target;
    public readonly byte[] Image;

    public SetHeadshotUiMessage(NetEntity target, byte[] image)
    {
        Target = target;
        Image = image;
    }
}
