using Content.Shared.ADT.Silicons.Borgs;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Silicons.Borgs;

public sealed partial class BorgSwitchableSubtypeSystem : SharedBorgSwitchableSubtypeSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableSubtypeComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, BorgSwitchableSubtypeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<SpriteSpecifier>(uid, BorgSwitchableSubtypeUiKey.Key, out var sprite))
            return;

        args.Sprite.LayerSetSprite(0, sprite);
    }
}
