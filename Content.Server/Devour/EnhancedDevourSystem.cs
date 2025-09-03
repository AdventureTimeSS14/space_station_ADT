using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.ADT.Silicon.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Shared.DoAfter;
using Robust.Shared.Maths;
using System.Numerics;
using Robust.Shared.Timing;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Devour;

/// <summary>
/// Enhanced devour system that handles predator-prey interactions with silicon restrictions,
/// consent mechanics, and improved digestion integration.
/// </summary>
public sealed class EnhancedDevourSystem : SharedDevourSystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private new readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private const float DevourRange = 1.5f;
    private static readonly SoundSpecifier DevourSound = new SoundPathSpecifier("/Audio/Effects/slurp.ogg");

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to devour events
        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDevourDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnGibContents);

        // Override the base devour action to use cursor targeting
        SubscribeLocalEvent<DevourerComponent, DevourActionEvent>(OnDevourAction);

        // Subscribe to custom action events
        SubscribeLocalEvent<PreyComponent, ToggleUnwillingConsentEvent>(OnToggleConsentAction);
    }

    // Note: DevourerComponent initialization is handled by the base SharedDevourSystem

    #region Event Handlers

    /// <summary>
    /// Overrides the base devour action to use cursor targeting and enhanced validation.
    /// </summary>
    private new void OnDevourAction(EntityUid uid, DevourerComponent component, DevourActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        // Prevent silicons from devouring
        if (HasComp<SiliconComponent>(uid))
        {
            _popupSystem.PopupEntity("devour-silicon-restriction", uid, uid);
            args.Handled = true;
            return;
        }

        // Validate prey
        if (!IsValidPrey(target, uid))
        {
            _popupSystem.PopupEntity("devour-invalid-prey", uid, uid);
            args.Handled = true;
            return;
        }

        // Check consent for unwilling devour
        if (!CanDevourUnwillingly(target, uid))
        {
            _popupSystem.PopupEntity("devour-consent-required", uid, uid);
            args.Handled = true;
            return;
        }

        // Check if predator has guts (required for enhanced devour)
        if (!TryComp<GutsComponent>(uid, out var guts))
        {
            _popupSystem.PopupEntity("devour-no-guts", uid, uid);
            args.Handled = true;
            return;
        }

        // Check if guts are full
        if (guts.CurrentAmount >= guts.MaxCapacity)
        {
            _popupSystem.PopupEntity("devour-guts-full", uid, uid);
            args.Handled = true;
            return;
        }

        // Start the devour do-after process
        StartDevourDoAfter(uid, target);
        args.Handled = true;
    }

    /// <summary>
    /// Handles the devour do-after completion event.
    /// </summary>
    private void OnDevourDoAfter(EntityUid uid, DevourerComponent component, DevourDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        // Prevent silicons from devouring
        if (HasComp<SiliconComponent>(uid))
        {
            _popupSystem.PopupEntity("devour-silicon-restriction", uid, uid);
            return;
        }

        // Validate prey
        if (!IsValidPrey(target, uid))
        {
            _popupSystem.PopupEntity("devour-invalid-prey", uid, uid);
            return;
        }

        // Check consent for unwilling devour
        if (!CanDevourUnwillingly(target, uid))
        {
            _popupSystem.PopupEntity("devour-consent-required", uid, uid);
            return;
        }

        // Perform devour
        PerformDevour(uid, target, component);
        args.Handled = true;
    }



    /// <summary>
    /// Handles toggle consent action event.
    /// </summary>
    private void OnToggleConsentAction(EntityUid uid, PreyComponent component, ToggleUnwillingConsentEvent args)
    {
        component.AllowUnwillingDevour = !component.AllowUnwillingDevour;

        var message = component.AllowUnwillingDevour
            ? "devour-consent-enabled"
            : "devour-consent-disabled";

        _popupSystem.PopupEntity(message, uid, uid);
    }

    /// <summary>
    /// Handles gib contents event to empty containers.
    /// </summary>
    private void OnGibContents(EntityUid uid, DevourerComponent component, ref BeingGibbedEvent args)
    {
        if (!component.ShouldStoreDevoured)
            return;

        // Empty stomach
        _containerSystem.EmptyContainer(component.Stomach);

        // Empty guts if present
        if (TryComp<GutsComponent>(uid, out var guts))
        {
            _containerSystem.EmptyContainer(guts.GutsContainer);
            guts.CurrentAmount = 0;
        }
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Performs the actual devour action.
    /// </summary>
    private void PerformDevour(EntityUid predator, EntityUid prey, DevourerComponent component)
    {
        // Play devour sound
        _audioSystem.PlayPvs(DevourSound, predator);

        // Store prey in stomach if configured
        if (component.ShouldStoreDevoured)
        {
            _containerSystem.Insert(prey, component.Stomach);

            // Show devour message to predator
            _popupSystem.PopupEntity(Loc.GetString("devour-success-predator", ("prey", Name(prey))), predator, predator);

            // Show devour message to prey
            _popupSystem.PopupEntity(Loc.GetString("devour-success-prey", ("predator", Name(predator))), prey, prey);

            // Start digestion process - move prey to guts after a delay
            StartDigestionProcess(predator, prey);
        }
        else
        {
            // If not storing, just delete the prey and add nutrients
            QueueDel(prey);
            _popupSystem.PopupEntity(Loc.GetString("devour-success-direct", ("prey", Name(prey))), predator, predator);
        }

        // Add healing chemicals to predator
        var ichorInjection = new Solution(component.Chemical, component.HealRate);
        _bloodstreamSystem.TryAddToChemicals(predator, ichorInjection);
    }

    /// <summary>
    /// Starts the digestion process by moving prey from stomach to guts.
    /// </summary>
    private void StartDigestionProcess(EntityUid predator, EntityUid prey)
    {
        // Ensure predator has guts component
        if (!TryComp<GutsComponent>(predator, out var guts))
            return;

        // Start a timer to move prey from stomach to guts
        Timer.Spawn(TimeSpan.FromSeconds(10), () =>
        {
            if (!Exists(predator) || !Exists(prey))
                return;

            // Move prey from stomach to guts
            if (TryComp<DevourerComponent>(predator, out var devourer) &&
                devourer.Stomach.Contains(prey))
            {
                _containerSystem.Remove(prey, devourer.Stomach);
                _containerSystem.Insert(prey, guts.GutsContainer);

                // Show digestion start messages
                _popupSystem.PopupEntity(Loc.GetString("digestion-start-predator", ("prey", Name(prey))), predator, predator);
                _popupSystem.PopupEntity(Loc.GetString("digestion-start-prey", ("predator", Name(predator))), prey, prey);
            }
        });
    }

    /// <summary>
    /// Starts the devour do-after process.
    /// </summary>
    private void StartDevourDoAfter(EntityUid predator, EntityUid prey)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, predator, TimeSpan.FromSeconds(3),
            new DevourDoAfterEvent(), predator, target: prey)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            NeedHand = false
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }



    /// <summary>
    /// Validates if an entity is valid prey.
    /// </summary>
    private bool IsValidPrey(EntityUid prey, EntityUid predator)
    {
        // Must have prey component
        if (!TryComp<PreyComponent>(prey, out var preyComp))
            return false;

        // Check if prey is undigestible
        if (preyComp.Undigestible)
            return false;

        // Check mob state
        if (!TryComp<MobStateComponent>(prey, out var mobState))
            return false;

        // Must be alive or dead (not critical)
        return mobState.CurrentState is MobState.Alive or MobState.Dead;
    }

    /// <summary>
    /// Checks if predator can devour prey unwillingly.
    /// </summary>
    private bool CanDevourUnwillingly(EntityUid prey, EntityUid predator)
    {
        if (!TryComp<PreyComponent>(prey, out var preyComp))
            return false;

        // If prey allows unwilling devour, always allow
        if (preyComp.AllowUnwillingDevour)
            return true;

        // Check if prey is willing (has predator component and is consenting)
        if (HasComp<PredatorComponent>(prey))
            return true;

        // Otherwise, consent is required
        return false;
    }

    /// <summary>
    /// Gets entities within a specified range of coordinates.
    /// </summary>
    private IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates center, float range)
    {
        // Simple implementation using EntityLookupSystem
        var lookupSystem = EntityManager.System<EntityLookupSystem>();
        return lookupSystem.GetEntitiesInRange(center, range);
    }

    #endregion
}
