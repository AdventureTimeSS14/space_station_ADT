using Content.Shared.Holiday;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Holiday;

public sealed class HolidaySystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _rescache = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HolidayRsiSwapComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<HolidayRsiSwapComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<string>(ent, HolidayVisuals.Holiday, out var data, args.Component))
            return;

        var comp = ent.Comp;
        if (!comp.Sprite.TryGetValue(data, out var rsistring) || args.Sprite == null)
            return;

        var path = SpriteSpecifierSerializer.TextureRoot / rsistring;
        if (_rescache.TryGetResource(path, out RSIResource? rsi))
        {
            _sprite.SetBaseRsi((ent.Owner, args.Sprite), rsi.RSI);
            // Workaround for RobustToolbox bug: SetBaseRsi doesn't mark bounds as dirty
            // Force bounds to be recalculated by touching layer states
            for (var i = 0; _sprite.TryGetLayer(ent.Owner, i, out _, false); i++)
            {
                if (_sprite.TryGetLayer(ent.Owner, i, out var layer, false) &&
                    layer.State.IsValid && layer.RSI == null)
                {
                    _sprite.LayerSetRsiState(ent.Owner, i, layer.State);
                }
            }
        }
    }
}
