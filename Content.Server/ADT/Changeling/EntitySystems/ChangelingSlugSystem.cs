using Content.Server.Actions;
using Content.Server.Store.Systems;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Popups;
using Content.Server.Traitor.Uplink;
using Content.Server.Body.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Polymorph;
using Content.Shared.Actions;
using Robust.Shared.Containers;
using Content.Shared.Alert;
using Content.Shared.Tag;
using Content.Shared.StatusEffect;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Server.Polymorph.Systems;
using Content.Shared.Mind;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Changeling.EntitySystems;

public sealed partial class LingSlugSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LingSlugComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<LingSlugComponent, MapInitEvent>(OnMapInit);

        InitializeLingAbilities();
    }
    private void OnStartup(EntityUid uid, LingSlugComponent component, ComponentStartup args)
    {
        RemComp<HungerComponent>(uid);
        RemComp<ThirstComponent>(uid);
    }
    private void OnMapInit(EntityUid uid, LingSlugComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.LayEggsActionEntity, component.LayEggsAction);
    }

    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";

    private bool LayEggs(EntityUid uid, EntityUid target, LingSlugComponent component)
    {
        if (!TryComp<MetaDataComponent>(target, out var metaData))
            return false;
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoidappearance))
        {
            return false;
        }

        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            _popup.PopupEntity(selfMessage, uid, uid);

            var targetMessage = Loc.GetString("changeling-sting-fail-target");
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, component.LayingDuration, new LingEggDoAfterEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
        return true;
    }

    public const string LingMonkeyId = "MobMonkeyChangeling";

    public bool SpawnLingMonkey(EntityUid uid, LingSlugComponent component)
    {
        if (component.EggLing != null)
        {
            var slug = Spawn(LingMonkeyId, Transform(component.EggLing.Value).Coordinates);

            var newLingComponent = EnsureComp<ChangelingComponent>(slug);
            newLingComponent.LesserFormActive = true;
            newLingComponent.AbsorbedDnaModifier = component.AbsorbedDnaModifier;


            _action.AddAction(slug, ref newLingComponent.ChangelingLesserFormActionEntity, newLingComponent.ChangelingLesserFormAction);


            newLingComponent.StoredDNA = new List<PolymorphHumanoidData>();    /// Создание нового ДНК списка
            if (component.EggLing != null)
            {
                var newHumanoidData = _polymorph.TryRegisterPolymorphHumanoidData(component.EggLing.Value);
                if (newHumanoidData == null)
                    return false;
                newLingComponent.StoredDNA.Add(newHumanoidData.Value);
            }
            else
                return false;

            if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
                _mindSystem.TransferTo(mindId, slug, mind: mind);
            if (mind != null)
                mind.PreventGhosting = false;
            return true;

        }
        else
            return false;
    }
}
