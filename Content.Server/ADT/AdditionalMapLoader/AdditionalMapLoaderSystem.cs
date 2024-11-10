using Content.Server.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

// Author: by TornadoTech
namespace Content.Server.ADT.AdditionalMapLoader;

public sealed class AdditionalMapLoaderSystem : EntitySystem
{
    [Dependency] private readonly GameTicking.GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadingMapsEvent>(OnGetMaps);
    }

    private void OnGetMaps(LoadingMapsEvent args)
    {
        var firstMap = args.Maps[0];
        if (!_prototype.TryIndex<AdditionalMapPrototype>(firstMap.ID, out var proto))
            return;

        foreach (var mapProtoId in proto.MapProtoIds)
        {
            if (!_prototype.TryIndex(mapProtoId, out var mapProto))
                continue;

            var map = _mapManager.CreateMap();
            _mapManager.AddUninitializedMap(map);
            _gameTicker.LoadGameMap(mapProto, map, null);
            _mapManager.DoMapInitialize(map);
        }
    }
}
