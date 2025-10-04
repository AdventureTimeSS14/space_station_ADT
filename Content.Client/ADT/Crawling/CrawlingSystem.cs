using Content.Shared.Standing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Crawling;

public sealed partial class CrawlingSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StandingStateComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, StandingStateComponent comp, AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_standing.IsDown(uid))
        {
            args.Sprite.DrawDepth = (int) DrawDepth.SmallMobs;
        }
        else
        {
            if (TryComp<SpriteComponent>(uid, out var spriteComp))
                args.Sprite.DrawDepth = spriteComp.DrawDepth;
            else
                args.Sprite.DrawDepth = (int) DrawDepth.Mobs;
        }
    }
}