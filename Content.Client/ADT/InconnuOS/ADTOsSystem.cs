using Content.Client.ADT.InconnuOS.UI;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.GameTicking;
using Robust.Shared.Reflection;

namespace Content.Client.ADT.InconnuOS;

public sealed class ADTOsSystem : SharedADTOsSystem
{
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;

    public OsAppRegistry Apps = default!;

    private readonly Dictionary<NetEntity, OsSession> _sessions = new();

    public override void Initialize()
    {
        base.Initialize();

        Apps = new OsAppRegistry(_reflection, _factory);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _sessions.Clear();
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
