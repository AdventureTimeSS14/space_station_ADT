using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.ADT.Silicons.Borgs;

public sealed partial class BorgSwitchableSubtypeSystem : SharedBorgSwitchableSubtypeSystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, BorgSubtypeChangedEvent>(OnSubtypeChanged);
    }
    private void OnSubtypeChanged(Entity<BorgSwitchableSubtypeComponent> ent, ref BorgSubtypeChangedEvent args)
    {
        SetAppearanceFromSubtype(ent, args.Subtype);
    }

    protected override void SetAppearanceFromSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        if (!_prototypeManager.TryIndex(subtype, out var subtypePrototype))
            return;

        if (!_prototypeManager.TryIndex(subtypePrototype.ParentBorgType, out var borgType))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        if (!_appearance.TryGetData<bool>(ent, BorgVisuals.HasPlayer, out var hasPlayer))
            hasPlayer = false;

        var rsiPath = SpriteSpecifierSerializer.TextureRoot / subtypePrototype.Sprite;

        if (_resourceCache.TryGetResource<RSIResource>(rsiPath, out var resource))
        {
            sprite.LayerSetState(BorgVisualLayers.Body, borgType.SpriteBodyState);
            sprite.LayerSetState(BorgVisualLayers.Light, hasPlayer ? borgType.SpriteHasMindState : borgType.SpriteNoMindState);
            sprite.LayerSetState(BorgVisualLayers.LightStatus, borgType.SpriteToggleLightState);

            sprite.LayerSetRSI(BorgVisualLayers.Body.GetHashCode(), resource.RSI);
            sprite.LayerSetRSI(BorgVisualLayers.Light.GetHashCode(), resource.RSI);
            sprite.LayerSetRSI(BorgVisualLayers.LightStatus.GetHashCode(), resource.RSI);
        }
    }
}
