using Content.Shared.ADT.ModSuits;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.ADT.ModSuits;

public sealed class ModSuitVoucherVisualizerSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ModSuitVoucherComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, ModSuitVoucherComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_appearance.TryGetData(uid, SuitVoucherVisuals.State, out SuitType state))
        {
            sprite.LayerSetState(0, state switch
            {
                SuitType.MOD => "mod",
                SuitType.Hard => "hard",
                _ => sprite.LayerGetState(0)
            });
        }
    }
}