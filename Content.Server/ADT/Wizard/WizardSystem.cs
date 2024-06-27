using Content.Server.Actions;
using Content.Shared.Inventory;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.ComponentalActions;
using Content.Shared.ComponentalActions.Components;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Server.Traitor.Uplink;
using Content.Server.Body.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Server.Polymorph.Systems;
using System.Linq;
using Content.Shared.Polymorph;
using Content.Server.Forensics;
using Content.Shared.Actions;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Alert;
using Content.Shared.Stealth.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Tag;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Mind;
using Robust.Shared.Audio;

namespace Content.Server.Wizard.EntitySystems;

public sealed partial class WizardSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly UplinkSystem _uplink = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GibbingSystem _gibbingSystem = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        InitializeWizardAbilities();
    }
}
