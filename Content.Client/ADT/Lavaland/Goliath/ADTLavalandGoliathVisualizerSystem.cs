using Content.Client.DamageState;
using Content.Shared.ADT.Lavaland.Goliath;
using Content.Shared.Mobs;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.ADT.Lavaland.Goliath;

public sealed class ADTLavalandGoliathVisualizerSystem : VisualizerSystem<ADTLavalandGoliathComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ADTLavalandGoliathComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (!SpriteSystem.LayerMapTryGet((uid, sprite), DamageStateVisualLayers.Base, out var layer, false))
            return;

        AppearanceSystem.TryGetData<MobState>(uid, MobStateVisuals.State, out var mobState, args.Component);
        if (SpriteSystem.LayerMapTryGet((uid, sprite), ADTLavalandGoliathVisualLayers.Glow, out var glow, false))
            SpriteSystem.LayerSetVisible((uid, sprite), glow, mobState != MobState.Dead);

        if (mobState == MobState.Dead)
        {
            SpriteSystem.LayerSetRsiState((uid, sprite), layer, component.DeadState);
            SpriteSystem.SetDrawDepth((uid, sprite), (int) DrawDepth.DeadMobs);
            SpriteSystem.SetColor((uid, sprite), Color.White);
            return;
        }

        SpriteSystem.SetDrawDepth((uid, sprite), (int) DrawDepth.Mobs);

        AppearanceSystem.TryGetData<ADTLavalandGoliathState>(uid, ADTLavalandGoliathVisuals.State, out var state, args.Component);
        var rsiState = state switch
        {
            ADTLavalandGoliathState.Aggro => component.AggroState,
            ADTLavalandGoliathState.PreAttack => component.PreAttackState,
            ADTLavalandGoliathState.WindUp => component.PreAttackState,
            _ => component.IdleState,
        };

        SpriteSystem.SetColor((uid, sprite), state == ADTLavalandGoliathState.WindUp ? component.WindUpColor : Color.White);

        SpriteSystem.LayerSetRsiState((uid, sprite), layer, rsiState);
    }
}
