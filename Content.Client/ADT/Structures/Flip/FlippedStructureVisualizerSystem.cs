using Content.Shared.ADT.Structures.Flip;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Structures.Flip;

public sealed partial class FlippedStructureVisualizerSystem : VisualizerSystem<FlippableStructureComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, FlippableStructureComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || component.FlippedPrototype != null)
            return;

        if (!AppearanceSystem.TryGetData<Angle>(uid, FlippedStructureVisuals.Tilt, out var tilt, args.Component))
            tilt = Angle.Zero;

        SpriteSystem.SetRotation((uid, args.Sprite), tilt);
    }
}
