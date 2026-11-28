using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ADTAddComponentsEffectSystem : EntityEffectSystem<MetaDataComponent, ADTAddComponents>
{
    [Dependency] private SharedPopupSystem _popup = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTAddComponents> args)
    {
        EntityManager.AddComponents(entity.Owner, args.Effect.Components);

        if (args.Effect.Popup is { } popup)
            _popup.PopupEntity(Loc.GetString(popup), entity.Owner, PopupType.Medium);
    }
}
