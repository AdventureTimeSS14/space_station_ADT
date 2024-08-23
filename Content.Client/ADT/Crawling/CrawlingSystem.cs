using Content.Shared.ADT.Crawling;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Crawling;

public sealed partial class CrawlingSystem : SharedCrawlingSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlerComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(EntityUid uid, CrawlerComponent comp, AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (_appearance.TryGetData<bool>(uid, CrawlingVisuals.Crawling, out var crawling, args.Component) && crawling)
        {
            args.Sprite.DrawDepth = (int) DrawDepth.SmallMobs;
        }
        else if (_appearance.TryGetData<bool>(uid, CrawlingVisuals.Standing, out var standing, args.Component) && standing)
        {
            if (TryPrototype(uid, out var proto) && proto.Components.TryGetComponent("SpriteComponent", out var sprite))    // TODO нормальное читание прототипа. Этот вариант не работает, потому маленьким мобам ползание не добавляем
            {
                var spriteComp = (SpriteComponent) sprite;
                if (spriteComp != null)
                    args.Sprite.DrawDepth = spriteComp.DrawDepth;
                else
                    args.Sprite.DrawDepth = (int) DrawDepth.Mobs;
            }
            else
                args.Sprite.DrawDepth = (int) DrawDepth.Mobs;
        }

    }
}
