using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Body.Limbs;
using Content.Shared.Clothing;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Body;

public sealed partial class LimbLossVisualsSystem : EntitySystem
{
    private const string ClipState = "regions";
    private const int ClipSize = 32;
    private const string GlovesSlot = "gloves";
    private const string ShoesSlot = "shoes";

    private static readonly ResPath ClipRsi = new("ADT/Mobs/Effects/limb_clip.rsi");

    private static readonly HashSet<string> ClippedSlots =
    [
        "jumpsuit",
        "outerClothing",
        "socks",
        "underwearb",
        "underweart",
        GlovesSlot,
        ShoesSlot,
    ];

    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private string GetClipState(string? species)
    {
        if (species == null || !_cache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / ClipRsi, out var rsi))
            return ClipState;

        var speciesState = $"{ClipState}-{species}";
        return rsi.RSI.TryGetState(speciesState, out _) ? speciesState : ClipState;
    }

    private void OnEquipmentVisualsUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (!ClippedSlots.Contains(args.Slot)
            || !TryComp<LimbLossComponent>(args.Equipee, out var loss)
            || loss.Missing == LimbParts.None
            || !TryComp<SpriteComponent>(args.Equipee, out var sprite))
            return;

        var body = (args.Equipee, sprite);
        var missing = GetClippedParts(loss.Missing, args.Slot);
        var clipState = GetClipState(CompOrNull<HumanoidProfileComponent>(args.Equipee)?.Species.Id);

        foreach (var key in args.RevealedLayers.ToArray())
        {
            if (!_sprite.LayerMapTryGet(body, key, out var index, false)
                || sprite[index] is not SpriteComponent.Layer layer
                || layer.CopyToShaderParameters != null
                || layer.ActualRsi is { } rsi && rsi.Size.X != ClipSize
                || GetClipShader(layer) is not { } clipShader)
                continue;

            sprite.LayerSetShader(key, clipShader);

            if (layer.Shader is not { } shader)
                continue;

            if (!shader.Mutable)
                layer.Shader = shader = shader.Duplicate();

            shader.SetParameter("clipArms", ToParameter(missing, LimbParts.ArmLeft, LimbParts.ArmRight, LimbParts.HandLeft, LimbParts.HandRight));
            shader.SetParameter("clipLegs", ToParameter(missing, LimbParts.LegLeft, LimbParts.LegRight, LimbParts.FootLeft, LimbParts.FootRight));

            var clipKey = $"{key}-adt-limb-clip";
            _sprite.AddLayer(body,
                new PrototypeLayerData
                {
                    RsiPath = ClipRsi.ToString(),
                    State = clipState,
                    CopyToShaderParameters = new PrototypeCopyToShaderParameters
                    {
                        LayerKey = key,
                        ParameterTexture = "clipMap",
                        ParameterUV = "clipUV",
                    },
                },
                index);
            _sprite.LayerMapSet(body, clipKey, index);
            args.RevealedLayers.Add(clipKey);
        }
    }

    private static LimbParts GetClippedParts(LimbParts missing, string slot)
    {
        switch (slot)
        {
            case GlovesSlot:
                if ((missing & LimbParts.HandLeft) != 0)
                    missing |= LimbParts.ArmLeft;
                if ((missing & LimbParts.HandRight) != 0)
                    missing |= LimbParts.ArmRight;
                break;
            case ShoesSlot:
                if ((missing & LimbParts.FootLeft) != 0)
                    missing |= LimbParts.LegLeft;
                if ((missing & LimbParts.FootRight) != 0)
                    missing |= LimbParts.LegRight;
                break;
        }

        return missing;
    }

    private static string? GetClipShader(SpriteComponent.Layer layer)
    {
        return layer.ShaderPrototype?.Id switch
        {
            null => "ADTLimbClip",
            "unshaded" => "ADTLimbClipUnshaded",
            "DisplacedDraw" or "DisplacedStencilDraw" => "ADTLimbClipDisplaced",
            "DisplacedDrawUnshaded" => "ADTLimbClipDisplacedUnshaded",
            _ => null,
        };
    }

    private static Vector4 ToParameter(LimbParts missing, LimbParts x, LimbParts y, LimbParts z, LimbParts w)
    {
        return new Vector4(
            (missing & x) != 0 ? 1f : 0f,
            (missing & y) != 0 ? 1f : 0f,
            (missing & z) != 0 ? 1f : 0f,
            (missing & w) != 0 ? 1f : 0f);
    }
}
