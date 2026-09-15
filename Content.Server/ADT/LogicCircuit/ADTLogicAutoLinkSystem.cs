using Content.Server.ADT.LogicCircuit.Components;
using Content.Server.DeviceLinking.Components;
using Content.Server.DeviceLinking.Systems;

namespace Content.Server.ADT.LogicCircuit;

public sealed class ADTLogicAutoLinkSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLogicAutoLinkComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ADTLogicAutoLinkComponent> ent, ref MapInitEvent args)
    {
        var grid = Transform(ent).GridUid;

        foreach (var link in ent.Comp.Links)
        {
            var linked = link.Outgoing
                ? LinkToReceivers(ent, grid, link)
                : LinkToTransmitters(ent, grid, link);

            if (!linked)
                Log.Warning($"Кабельная коробка {ToPrettyString(ent)} не нашла канал {link.Channel}.");
        }
    }

    private bool LinkToReceivers(EntityUid box, EntityUid? grid, ADTLogicAutoLinkEntry link)
    {
        var linked = false;
        var query = EntityQueryEnumerator<AutoLinkReceiverComponent>();

        while (query.MoveNext(out var target, out var marker))
        {
            if (marker.AutoLinkChannel != link.Channel || !SameGrid(target, grid))
                continue;

            _deviceLink.ToggleLink(null, box, target, link.OwnPort, link.TargetPort);
            linked = true;
        }

        return linked;
    }

    private bool LinkToTransmitters(EntityUid box, EntityUid? grid, ADTLogicAutoLinkEntry link)
    {
        var linked = false;
        var query = EntityQueryEnumerator<AutoLinkTransmitterComponent>();

        while (query.MoveNext(out var target, out var marker))
        {
            if (marker.AutoLinkChannel != link.Channel || !SameGrid(target, grid))
                continue;

            _deviceLink.ToggleLink(null, target, box, link.TargetPort, link.OwnPort);
            linked = true;
        }

        return linked;
    }

    private bool SameGrid(EntityUid target, EntityUid? grid)
    {
        return Transform(target).GridUid == grid;
    }
}
