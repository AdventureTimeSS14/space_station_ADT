using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Content.Server.Atmos.Piping.Components;
using Content.Server.NodeContainer;
using Content.Server.NodeContainer.Nodes;
using Content.Shared.NodeContainer;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.ADT.Atmos.Piping.Components;
using Content.Server.Atmos.Components;

namespace Content.Server.ADT.Atmos.Piping.System;

public sealed class PipeLeaksSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PipeLeakingComponent, ComponentStartup>(OnPipeStartup);
        SubscribeLocalEvent<PipeLeakingComponent, AnchorStateChangedEvent>(OnPipeAnchorChanged);
    }

    private void OnPipeStartup(EntityUid uid, PipeLeakingComponent component, ref ComponentStartup args)
    {
        CheckPipeConnections(uid, component);
    }

    private void OnPipeAnchorChanged(EntityUid uid, PipeLeakingComponent component, ref AnchorStateChangedEvent args)
    {
        Timer.Spawn(10, () => CheckPipeConnections(uid, component));
    }

    private void CheckPipeConnections(EntityUid uid, PipeLeakingComponent pipe)
    {
        if (Deleted(uid))
            return;

        if (!TryComp<NodeContainerComponent>(uid, out var container))
            return;

        PipeNode? pipeNode = null;

        // Знаходимо PipeNode
        foreach (var node in container.Nodes.Values)
        {
            if (node is PipeNode pn)
            {
                pipeNode = pn;
                break;
            }
        }

        if (pipeNode == null)
            return;

        // Проверяем, есть ли соединения с другими сущностями
        bool hasConnection = pipeNode.ReachableNodes.Any(x => x.Owner != uid);

        if (!hasConnection)
        {
            var ventComponent = EnsureComp<GasPassiveVentComponent>(uid);
            EnsureComp<AtmosDeviceComponent>(uid);
            
            // Передаем модификатор для "бочок потик". Захардкодил, ибо не вижу смысла выводить это значение в компоненты. Максимум в CCvar (но тоже смысла 0).
            ventComponent.Multiplier = 0.2f; // FUCKING HARDCODE
        }
        else
        {
            RemComp<GasPassiveVentComponent>(uid);
            RemComp<AtmosDeviceComponent>(uid);
        }
    }
}