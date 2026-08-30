using Content.Shared.ADT.Areas;
using Content.Shared.ADT.TTS;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.TTS;

public sealed partial class TTSSystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    private string? ResolveEffect(EntityUid uid, TTSComponent component)
    {
        var effect = TryGetAreaEffect(uid, out var areaEffect)
            ? areaEffect
            : component.Effect;

        return string.IsNullOrEmpty(effect) ? null : effect;
    }

    private bool TryGetAreaEffect(EntityUid uid, out string effect)
    {
        effect = string.Empty;

        if (_area.GetAreaPrototypeId(Transform(uid).Coordinates) is not { } areaId)
            return false;

        if (!_prototypeManager.TryIndex((EntProtoId)areaId, out var areaProto))
            return false;

        if (!areaProto.TryGetComponent<TTSAreaEffectComponent>(out var areaEffect, _componentFactory))
            return false;

        effect = areaEffect.Effect;
        return true;
    }
}
