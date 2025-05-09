using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.ADT.Silicons.Borgs.Components;
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
    private ISawmill _sawmill = default!;

    protected override void SetAppearanceFromSubtype(Entity<BorgSwitchableSubtypeComponent> ent, ProtoId<BorgSubtypePrototype> subtype)
    {
        if (!_prototypeManager.TryIndex(subtype, out var subtypePrototype))
        {
            _sawmill.Warning($"Failed to find BorgSubtypePrototype with ID {subtype}");
            return;
        }

        if (!_prototypeManager.TryIndex(subtypePrototype.ParentBorgType, out var borgType))
        {
            _sawmill.Warning($"Failed to find parent BorgTypePrototype with ID {subtypePrototype.ParentBorgType}");
            return;
        }

        if (!TryComp(ent, out SpriteComponent? sprite))
        {
            _sawmill.Warning($"Entity {ent} lacks a SpriteComponent, cannot update appearance");
            return;
        }

        if (!_appearance.TryGetData<bool>(ent, BorgVisuals.HasPlayer, out var hasPlayer))
            hasPlayer = false;

        var rsiPath = SpriteSpecifierSerializer.TextureRoot / subtypePrototype.Sprite;

        if (_resourceCache.TryGetResource<RSIResource>(rsiPath, out var resource))
        {
            var bodyState = string.IsNullOrEmpty(borgType.SpriteBodyState)
                ? borgType.SpriteBodyState
                : borgType.SpriteBodyState;
            var toggleLightState = string.IsNullOrEmpty(borgType.SpriteToggleLightState)
                ? borgType.SpriteToggleLightState
                : borgType.SpriteToggleLightState;
            var hasMindState = string.IsNullOrEmpty(borgType.SpriteHasMindState)
                ? borgType.SpriteHasMindState
                : borgType.SpriteHasMindState;
            var noMindState = string.IsNullOrEmpty(borgType.SpriteNoMindState)
                ? borgType.SpriteNoMindState
                : borgType.SpriteNoMindState;

            sprite.LayerSetState(BorgVisualLayers.Body, bodyState);
            sprite.LayerSetState(BorgVisualLayers.Light, hasPlayer ? hasMindState : noMindState);
            sprite.LayerSetState(BorgVisualLayers.LightStatus, toggleLightState);

            sprite.LayerSetRSI(BorgVisualLayers.Body, resource.RSI);
            sprite.LayerSetRSI(BorgVisualLayers.Light, resource.RSI);
            sprite.LayerSetRSI(BorgVisualLayers.LightStatus, resource.RSI);
        }

        SetSubtype(ent, subtypePrototype);
    }
}
