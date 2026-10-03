using Content.Client.ADT.InconnuOS.UI;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.GameTicking;
using Robust.Shared.Network;
using Robust.Shared.Reflection;

namespace Content.Client.ADT.InconnuOS;

public sealed class ADTOsSystem : SharedADTOsSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private readonly IClientNetManager _netManager = default!;

    public OsAppRegistry Apps = default!;

    private readonly Dictionary<NetEntity, OsSession> _sessions = new();

    public event Action? RoundRestarting;

    public override void Initialize()
    {
        base.Initialize();

        Apps = new OsAppRegistry(_reflection, _factory);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        _netManager.Disconnect += OnDisconnect;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _netManager.Disconnect -= OnDisconnect;
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        RoundRestarting?.Invoke();
        _sessions.Clear();
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs args)
    {
        RoundRestarting?.Invoke();
    }

    public OsSession? GetSession(EntityUid machine)
    {
        return _sessions.GetValueOrDefault(GetNetEntity(machine));
    }

    public void SaveSession(EntityUid machine, OsSession session)
    {
        _sessions[GetNetEntity(machine)] = session;
    }

    public void ClearSession(EntityUid machine)
    {
        _sessions.Remove(GetNetEntity(machine));
    }
}
