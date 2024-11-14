using Content.Server.Actions;
using Content.Server.Store.Systems;
using Content.Shared.NarcoticEffects.Components;
using Content.Shared.Popups;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Alert;
using Content.Shared.Tag;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using Content.Shared.Eye;
using Content.Shared.Movement.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Gibbing.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.EntitySystems;

public sealed partial class NarcoHallucinationsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly VisibilitySystem _visibilitySystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public static string HallucinatingKey = "Hallucinations";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarcoHallucinationsComponent, MapInitEvent>(OnHallucinationsInit);
        SubscribeLocalEvent<NarcoHallucinationsComponent, ComponentShutdown>(OnHallucinationsShutdown);
    }

    private void OnHallucinationsInit(EntityUid uid, NarcoHallucinationsComponent component, MapInitEvent args)
    {
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask | (int) VisibilityFlags.Narcotic, eye);
    }

    private void OnHallucinationsShutdown(EntityUid uid, NarcoHallucinationsComponent component, ComponentShutdown args)
    {
        if (!_entityManager.TryGetComponent<EyeComponent>(uid, out var eye))
            return;
        _eye.SetVisibilityMask(uid, eye.VisibilityMask & ~(int) VisibilityFlags.Narcotic, eye);
    }
}
