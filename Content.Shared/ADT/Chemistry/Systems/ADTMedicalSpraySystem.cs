using Content.Shared.Administration.Logs;
using Content.Shared.ADT.Chemistry.Events;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chemistry.Systems;

public sealed class ADTMedicalSpraySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public bool TryBlockExposedSkin(Entity<InjectorComponent> injector, EntityUid user, EntityUid target)
    {
        if (!_proto.Resolve(injector.Comp.ActiveModeProtoId, out var mode) || !mode.RequiresExposedSkin)
            return false;

        var skinEv = new ExposedSkinAttemptEvent(injector.Owner, target);
        RaiseLocalEvent(ref skinEv);
        if (!skinEv.Cancelled)
            return false;

        _popup.PopupClient(Loc.GetString(skinEv.CancelMessage), target, user);
        _popup.PopupEntity(Loc.GetString(skinEv.CancelMessage), target, target);
        return true;
    }

    public bool TryApplyTopical(Entity<InjectorComponent> injector, EntityUid user, EntityUid target, ReactionMethod method)
    {
        if (!_solutionContainer.ResolveSolution(injector.Owner, injector.Comp.SolutionName, ref injector.Comp.Solution, out var injectorSolution)
            || injectorSolution.Volume == 0)
        {
            _popup.PopupClient(Loc.GetString("injector-component-empty-message", ("injector", injector)), user, user);
            return false;
        }

        var amount = FixedPoint2.Min(injector.Comp.CurrentTransferAmount ?? injectorSolution.Volume, injectorSolution.Volume);
        var removedSolution = _solutionContainer.SplitSolution(injector.Comp.Solution.Value, amount);

        _reactive.DoEntityReaction(target, removedSolution, method);

        _popup.PopupClient(Loc.GetString("injector-component-inject-success-message", ("amount", removedSolution.Volume), ("target", Identity.Entity(target, EntityManager))), target, user);

        if (_proto.Resolve(injector.Comp.ActiveModeProtoId, out var activeMode))
        {
            if (activeMode.InjectPopupTarget != null && target != user)
                _popup.PopupClient(Loc.GetString(activeMode.InjectPopupTarget), target, target);

            if (activeMode.InjectSound != null)
                _audio.PlayPredicted(activeMode.InjectSound, injector, user);
        }

        _adminLogger.Add(LogType.ForceFeed, $"{ToPrettyString(user):user} applied {ToPrettyString(target):target} with a solution {SharedSolutionContainerSystem.ToPrettyString(removedSolution):removedSolution} using a {ToPrettyString(injector):using}");

        _useDelay.TryResetDelay(injector);
        return true;
    }

    public bool ShouldBlockSplash(Solution solution)
    {
        foreach (var reagentQuantity in solution.Contents)
        {
            if (_proto.TryIndex<ReagentPrototype>(reagentQuantity.Reagent.Prototype, out var reagent)
                && reagent.SplashBlocked)
            {
                return true;
            }
        }

        return false;
    }
}