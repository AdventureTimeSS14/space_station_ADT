using Content.Client.DamageState;
using Content.Shared.ADT.Salvage;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Salvage;

public sealed class MegafaunaVisualsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MegafaunaComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, MegafaunaComponent comp, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
        if (!TryComp<DamageStateVisualsComponent>(uid, out var state))
            return;
        if (!TryComp<MobStateComponent>(uid, out var mob))
            return;
        if (!state.States.TryGetValue(mob.CurrentState, out var layers))
            return;

        if (_appearance.TryGetData<bool>(uid, AshdrakeVisuals.Swoop, out var swoop))
        {
            if (!sprite.LayerMapTryGet("drake_swoop", out var index))
                index = sprite.LayerMapReserveBlank("drake_swoop");
            sprite.LayerSetState(index, "swoop");
            sprite.LayerSetVisible(index, swoop);

            foreach (var (key, _) in layers)
            {
                if (!sprite.LayerMapTryGet(key, out var layer)) continue;
                sprite.LayerSetVisible(layer, !swoop);
            }
        }
    }
}
