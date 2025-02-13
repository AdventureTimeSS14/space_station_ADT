using Content.Shared.Chat;

namespace Content.Client.Chat.Managers
{
    public interface IChatManager : ISharedChatManager
    {
        void Initialize(); // ADT-CollectiveMind-Tweak
        /// <summary>
        ///     Will refresh perms.
        /// </summary>
        event Action PermissionsUpdated; // ADT-CollectiveMind-Tweak
        public void UpdatePermissions(); // ADT-CollectiveMind-Tweak
        public void SendMessage(string text, ChatSelectChannel channel);
    }
}
