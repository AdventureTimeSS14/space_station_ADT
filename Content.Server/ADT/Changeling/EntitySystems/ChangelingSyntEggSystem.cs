using Content.Shared.Changeling.Components;
using Content.Server.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Alert;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;
using Content.Server.Resist;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class ChangelingSyntEggSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SyntLingEggsHolderComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<SyntLingEggsHolderComponent, ComponentShutdown>(OnShutdown);

    }
    public const string LingSlugId = "ChangelingHeadslug";
    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Genetic";
    private void OnInit(EntityUid uid, SyntLingEggsHolderComponent component, MapInitEvent args)
    {
        var slug = Spawn(LingSlugId, Transform(uid).Coordinates);
        var slugComp = EnsureComp<LingSlugComponent>(slug);
        slugComp.EggLing = uid;
        var xform = Transform(uid);
        _transform.SetParent(slug, xform.ParentUid);
        _transform.SetCoordinates(slug, xform.Coordinates);
        component.Stomach = ContainerSystem.EnsureContainer<Container>(uid, "stomach");
        ContainerSystem.Insert(slug, component.Stomach);
        RemComp<CanEscapeInventoryComponent>(slug);

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, slug, mind: mind);
        _action.RemoveAction(slug, slugComp.LayEggsActionEntity);   /// Яйца откладываются только один раз
        _action.AddAction(slug, ref component.ChangelingHatchActionEntity, component.ChangelingHatchAction);

        var damage_burn = new DamageSpecifier(_proto.Index(BruteDamageGroup), component.DamageAmount);
        _damageableSystem.TryChangeDamage(uid, damage_burn);    /// To be sure that target is dead
        var newLing = EnsureComp<ChangelingComponent>(uid);
        newLing.EggedBody = true;       /// To make egged person into a ling
        var selfMessage = Loc.GetString("changeling-eggs-inform");
        _popup.PopupEntity(selfMessage, uid, uid, PopupType.LargeCaution);      /// Popup
    }
    private void OnShutdown(EntityUid uid, SyntLingEggsHolderComponent component, ComponentShutdown args)
    {
        RemComp<ChangelingComponent>(uid);
        //_action.RemoveAction(uid, component.ChangelingHatchActionEntity);
    }
}
