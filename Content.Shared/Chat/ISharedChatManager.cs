namespace Content.Shared.Chat;

public interface ISharedChatManager
{
    void Initialize();
    void SendAdminAlert(string message);
    void SendAdminAlert(string message, Color? colorOverride); // ADT-Tweak
    void SendAdminAlert(EntityUid player, string message);
}
