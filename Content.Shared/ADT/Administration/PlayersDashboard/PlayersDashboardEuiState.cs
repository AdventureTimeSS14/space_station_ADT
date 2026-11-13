using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Administration.PlayersDashboard;

[Serializable, NetSerializable]
public sealed class PlayersDashboardEuiState : EuiStateBase
{
    public int Total;
    public int Queue;
    public int Lobby;
    public int Ghosts;
    public int AdminGhosts;
    public int InGame;
    public int Other;
    public int Admins;
}
