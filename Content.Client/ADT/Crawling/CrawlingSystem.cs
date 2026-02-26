using Content.Shared.Standing;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Crawling;

public sealed partial class CrawlingSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;
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

        _sprite.SetDrawDepth(uid, _standing.IsDown(uid) ? (int)DrawDepth.SmallMobs : (int)DrawDepth.Mobs);
    }
}
