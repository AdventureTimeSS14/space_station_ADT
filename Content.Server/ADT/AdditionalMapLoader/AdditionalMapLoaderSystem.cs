using Content.Server.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

// Author: by TornadoTech
namespace Content.Server.ADT.AdditionalMapLoader;

public sealed class AdditionalMapLoaderSystem : EntitySystem
{
    [Dependency] private readonly GameTicking.GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LoadingMapsEvent>(OnGetMaps);
    }

    private void OnGetMaps(LoadingMapsEvent args)
    {
        var firstMap = args.Maps[0];
        if (!_prototypeManager.TryIndex<AdditionalMapPrototype>(firstMap.ID, out var proto))
            return;

        foreach (var mapProtoId in proto.MapProtoIds)
        {
            if (!_prototypeManager.TryIndex(mapProtoId, out var mapProto))
            {
                Log.Error($"Prototype not found with ID '{mapProtoId.Id}' in '{proto.ID}'. " +
                $"Please ensure this prototype exists in '- type: additionalMap' check `maps:`!!");
                continue;
            }
            CreateAndInitializeMap(mapProto);
        }
    }
    private void CreateAndInitializeMap(Maps.GameMapPrototype mapProto)
    {
        var map = _mapManager.CreateMap();
        _mapManager.AddUninitializedMap(map);
        _gameTicker.LoadGameMap(mapProto, map, null);
        _mapManager.DoMapInitialize(map);
    }
}
