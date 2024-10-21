using Content.Shared.Changeling.Components;
using Content.Server.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Alert;
using Robust.Shared.Containers;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingEggSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LingEggsHolderComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<LingEggsHolderComponent, ComponentShutdown>(OnShutdown);

    }
    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Genetic";
    private void OnInit(EntityUid uid, LingEggsHolderComponent component, MapInitEvent args)
    {
        component.Stomach = ContainerSystem.EnsureContainer<Container>(uid, "stomach");
        var damage_burn = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.DamageAmount);
        _damageableSystem.TryChangeDamage(uid, damage_burn);    /// To be sure that target is dead
        var newLing = EnsureComp<ChangelingComponent>(uid);
        newLing.EggedBody = true;       /// To make egged person into a ling
        var selfMessage = Loc.GetString("changeling-eggs-inform");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.LargeCaution);      /// Popup
    }
    private void OnShutdown(EntityUid uid, LingEggsHolderComponent component, ComponentShutdown args)
    {
        RemComp<ChangelingComponent>(uid);
        //_action.RemoveAction(uid, component.ChangelingHatchActionEntity);
    }
}
