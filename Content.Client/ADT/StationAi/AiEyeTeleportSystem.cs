using Content.Shared.ADT.StationAi;
using Robust.Client.Network;
using Robust.Shared.Network;

namespace Content.Client.ADT.StationAi;

public sealed class AiEyeTeleportSystem : EntitySystem
{
    [Dependency] private readonly IClientNetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        _net.RegisterNetMessage<MsgAiEyeTeleport>();
    }
}
