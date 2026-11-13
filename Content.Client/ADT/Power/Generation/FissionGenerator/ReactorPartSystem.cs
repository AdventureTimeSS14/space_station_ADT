using Content.Shared.ADT.Power.Generation.FissionGenerator;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Power.Generation.FissionGenerator;

// ADT: адаптировано под ADT — убран heat-distortion постшейдер (в ADT другой API шейдеров),
// оставлена только tint-подсветка по цвету материала.
public sealed partial class ReactorPartSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReactorPartComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<ReactorPartComponent, ComponentInit>(OnComponentInit);
    }

    private void OnAppearanceChange(EntityUid uid, ReactorPartComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        _sprite.LayerSetColor((uid, args.Sprite), 0, _proto.Index(component.Material).Color);
    }

    private void OnComponentInit(Entity<ReactorPartComponent> ent, ref ComponentInit args)
        => _sprite.LayerSetColor((ent.Owner, Comp<SpriteComponent>(ent.Owner)), 0, _proto.Index(ent.Comp.Material).Color);
}
