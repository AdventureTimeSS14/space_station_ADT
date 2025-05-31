using Content.Shared.ADT.Slimecats;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Slimecats;

public sealed partial class SlimecatsSleepActionSystem : EntitySystem
{

    [Dependency] private readonly AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SharedSlimecatsSleepActionComponent, AppearanceChangeEvent>(ChangeAppearanceSprite);
        SubscribeLocalEvent<SharedSlimecatsSleepActionComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, SharedSlimecatsSleepActionComponent component, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerSetVisible(0, true);  // Основной спрайт виден
            sprite.LayerSetVisible(1, false); // Слой сна скрыт
        }
    }

    public void ChangeAppearanceSprite(EntityUid uid, SharedSlimecatsSleepActionComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_appearance.TryGetData<bool>(uid, StateSlimcatVisual.Sleep, out var isSleeping))
        {
            sprite.LayerSetVisible(0, !isSleeping); // Слой 0 (стоит) виден, когда котик не спит
            sprite.LayerSetVisible(1, isSleeping);  // Слой 1 (сон) виден, когда котик спит
        }
    }
}