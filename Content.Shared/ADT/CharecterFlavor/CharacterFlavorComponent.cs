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
    /// ссылка на изображение, что будет использоваться в качестве хэдшота
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string HeadshotUrl = string.Empty;
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

/// <summary>
/// Запрос предпросмотра хэдшота (например, из лобби).
/// Клиент не может ходить в интернет напрямую, поэтому просит сервер скачать картинку.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RequestHeadshotPreviewEvent : EntityEventArgs
{
    public readonly string Url;

    public RequestHeadshotPreviewEvent(string url)
    {
        Url = url;
    }
}

/// <summary>
/// Ответ сервера с уже загруженным изображением хэдшота для предпросмотра.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class HeadshotPreviewEvent : EntityEventArgs
{
    public readonly byte[] Image;

    public HeadshotPreviewEvent(byte[] image)
    {
        Image = image;
    }
}
