using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Noise;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Parallax;

[Serializable, NetSerializable]
public sealed partial class ADTBiomeCaveLayer : IBiomeWorldLayer
{
    [DataField]
    public List<ProtoId<ContentTileDefinition>> AllowedTiles { get; private set; } = new();

    public FastNoiseLite Noise { get; } = new();

    public float Threshold => -1f;

    public bool Invert => false;
}
