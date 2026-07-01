using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Silicons.Borgs;

public abstract class SharedAiRemoteControlSystem : EntitySystem
{
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public void ReturnMindIntoAi(EntityUid entity)
    {
        if (!TryComp<AiRemoteControllerComponent>(entity, out var remoteComp)
            || remoteComp.AiHolder == null
            || !_stationAiSystem.TryGetCore(remoteComp.AiHolder.Value, out var stationAiCore)
            || stationAiCore.Comp?.RemoteEntity == null
            || remoteComp.LinkedMind == null
            || !TryComp<StationAiHeldComponent>(remoteComp.AiHolder.Value, out var stationAiHeldComp))
            return;

        stationAiHeldComp.CurrentConnectedEntity = null;

        _mind.TransferTo(remoteComp.LinkedMind.Value, remoteComp.AiHolder);

        _stationAiSystem.SwitchRemoteEntityMode(stationAiCore!, true);
        remoteComp.AiHolder = null;
        remoteComp.LinkedMind = null;

        _xformSystem.SetCoordinates(stationAiCore.Comp.RemoteEntity.Value, Transform(entity).Coordinates);
    }
}
public sealed partial class ReturnMindIntoAiEvent : InstantActionEvent { public RemoteDevicesUiKey Key; }

[Serializable, NetSerializable]
public enum RemoteDevicesUiKey : byte
{
    Key
}

public sealed partial class ToggleRemoteDevicesScreenEvent : InstantActionEvent { public RemoteDevicesUiKey Key; }