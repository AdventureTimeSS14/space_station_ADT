using Robust.Shared.GameStates;
using Content.Shared.Chat;

namespace Content.Shared.Emoting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmotingComponent : Component
{
    [DataField, AutoNetworkedField]
    [Access(typeof(EmoteSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
    public bool Enabled = true;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChatEmoteCooldown = TimeSpan.FromSeconds(0.5); // ganimed edit

    [ViewVariables]
    [Access(typeof(SharedChatSystem), Friend = AccessPermissions.ReadWrite, Other = AccessPermissions.Read)]
    public TimeSpan? LastChatEmoteTime; // ganimed edit
}
