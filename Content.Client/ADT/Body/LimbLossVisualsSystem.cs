using System.Linq;
using System.Numerics;
using Content.Client.Body;
using Content.Client.Clothing;
using Content.Shared.ADT.Body.Limbs;
using Content.Shared.Clothing;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Body;

public sealed partial class LimbLossVisualsSystem : EntitySystem
{
    private const string ClipState = "regions";
    private const string ClipSuffix = "-adt-limb-clip";
    private const string NoLegsSuffix = "_nolegs";
    private const int ClipSize = 32;
    private const string GlovesSlot = "gloves";
    private const string ShoesSlot = "shoes";
    private const LimbParts AllParts = LimbParts.Hands | LimbParts.Legs;

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

    private static readonly Dictionary<HumanoidVisualLayers, LimbParts> MarkingClipParts = new()
    {
        { HumanoidVisualLayers.Chest, AllParts },
        { HumanoidVisualLayers.Special, AllParts },
        { HumanoidVisualLayers.Overlay, AllParts },
        { HumanoidVisualLayers.UndergarmentTop, AllParts },
        { HumanoidVisualLayers.UndergarmentBottom, AllParts },
        { HumanoidVisualLayers.LArm, LimbParts.HandLeft },
        { HumanoidVisualLayers.RArm, LimbParts.HandRight },
        { HumanoidVisualLayers.LLeg, LimbParts.FootLeft },
        { HumanoidVisualLayers.RLeg, LimbParts.FootRight },
    };

    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private ClientClothingSystem _clothing = default!;
    [Dependency] private MarkingManager _marking = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private VisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
        SubscribeLocalEvent<LimbLossComponent, AfterAutoHandleStateEvent>(OnLimbLossState);
    }

    private void OnLimbLossState(Entity<LimbLossComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _visualBody.RefreshMarkings(ent);

        if (TryComp<InventoryComponent>(ent, out var inventory))
            _clothing.InitClothing(ent, inventory);
    }

    public void ApplyMarkings(Entity<SpriteComponent> body, List<Marking> markings)
    {
        if (!TryComp<LimbLossComponent>(body, out var loss) || loss.Missing == LimbParts.None)
            return;

        var legsMissing = (loss.Missing & LimbParts.Legs) != 0;
        var clipState = GetClipState(CompOrNull<HumanoidProfileComponent>(body)?.Species.Id);

        foreach (var marking in markings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            var parts = MarkingClipParts.GetValueOrDefault(proto.BodyPart) & loss.Missing;

            foreach (var specifier in proto.Sprites)
            {
                if (specifier is not SpriteSpecifier.Rsi rsi)
                    continue;

                var key = $"{proto.ID}-{rsi.RsiState}";

                if (legsMissing)
                    TrySetNoLegsState(body, key, rsi.RsiState);

                if (parts != LimbParts.None)
                    AddClip(body, key, parts, clipState);
            }
        }
    }

    public void RemoveMarkings(Entity<SpriteComponent> body, List<Marking> markings)
    {
        foreach (var marking in markings)
        {
            if (!_marking.TryGetMarking(marking, out var proto))
                continue;

            foreach (var specifier in proto.Sprites)
            {
                if (specifier is SpriteSpecifier.Rsi rsi)
                    _sprite.RemoveLayer(body.AsNullable(), $"{proto.ID}-{rsi.RsiState}{ClipSuffix}", false);
            }
        }
    }

    private void TrySetNoLegsState(Entity<SpriteComponent> body, string key, string state)
    {
        if (!_sprite.LayerMapTryGet(body.AsNullable(), key, out var index, false)
            || _sprite.LayerGetEffectiveRsi(body.AsNullable(), index) is not { } rsi)
            return;

        var noLegs = state + NoLegsSuffix;
        if (rsi.TryGetState(noLegs, out _))
            _sprite.LayerSetRsiState(body.AsNullable(), index, noLegs);
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
            if (AddClip(body, key, missing, clipState) is { } clipKey)
                args.RevealedLayers.Add(clipKey);
        }
    }

    private string? AddClip(Entity<SpriteComponent> body, string key, LimbParts missing, string clipState)
    {
        var sprite = body.Comp;
        var clipKey = key + ClipSuffix;

        if (_sprite.LayerMapTryGet(body.AsNullable(), clipKey, out _, false)
            || !_sprite.LayerMapTryGet(body.AsNullable(), key, out var index, false)
            || sprite[index] is not SpriteComponent.Layer layer
            || layer.CopyToShaderParameters != null
            || layer.ActualRsi is { } rsi && rsi.Size.X != ClipSize
            || GetClipShader(layer) is not { } clipShader)
            return null;

        sprite.LayerSetShader(key, clipShader);

        if (layer.Shader is not { } shader)
            return null;

        if (!shader.Mutable)
            layer.Shader = shader = shader.Duplicate();

        shader.SetParameter("clipArms", ToParameter(missing, LimbParts.ArmLeft, LimbParts.ArmRight, LimbParts.HandLeft, LimbParts.HandRight));
        shader.SetParameter("clipLegs", ToParameter(missing, LimbParts.LegLeft, LimbParts.LegRight, LimbParts.FootLeft, LimbParts.FootRight));

        _sprite.AddLayer(body.AsNullable(),
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
        _sprite.LayerMapSet(body.AsNullable(), clipKey, index);

        return clipKey;
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
