// SPDX-FileCopyrightText: 2026 ultradyper
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.SlimeBody;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.SlimeBody;

/// <summary>
/// Lets slime people refill their blood by drinking their own body reagent:
/// the reagent is slowly transferred from the stomach into the bloodstream.
/// </summary>
public sealed partial class ADTSlimeBodySystem : SharedADTSlimeBodySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;

    private const float TransferPerSecond = 2.5f;
    private const float BloodRegenPerTick = 6f;
    private static readonly TimeSpan BloodRegenInterval = TimeSpan.FromSeconds(3);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ADTSlimeBodyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.Profile.Species != "SlimePerson")
            return;

        var composition = SlimeBodyCompositions.Get(args.Profile.SlimeBodyComposition);
        if (composition is null)
            return;

        ApplyComposition(args.Mob, composition);
    }

    /// <summary>
    /// Changes the entity's blood to the composition reagent and marks it as a liquid body.
    /// </summary>
    public void ApplyComposition(EntityUid uid, SlimeBodyComposition composition)
    {
        if (!TryComp(uid, out BloodstreamComponent? bloodstream))
            return;

        var volume = bloodstream.BloodReferenceSolution.Volume;
        _bloodstream.ChangeBloodReagents(uid, new Solution(new[] { new ReagentQuantity(composition.Reagent, volume) }));

        if (!TryComp(uid, out ADTSlimeBodyComponent? slimeBody))
        {
            AddComp(uid, new ADTSlimeBodyComponent { Reagent = composition.Reagent });
            slimeBody = Comp<ADTSlimeBodyComponent>(uid);
        }
        else
        {
            slimeBody.Reagent = composition.Reagent;
            Dirty(uid, slimeBody);
        }

        InitializeSlimeBody(uid, slimeBody);
    }

    /// <summary>
    /// Sets up timers and finds the stomach organ. Called on MapInit and right
    /// after the component is added at player spawn (MapInit does not fire for
    /// components added after the entity was initialized).
    /// </summary>
    private void InitializeSlimeBody(EntityUid uid, ADTSlimeBodyComponent component)
    {
        component.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(1);
        component.BloodRegenNextUpdate = _timing.CurTime + BloodRegenInterval;

        var stomachQuery = EntityQueryEnumerator<StomachComponent, OrganComponent>();
        while (stomachQuery.MoveNext(out var organUid, out _, out var organ))
        {
            if (organ.Body == uid)
            {
                component.StomachOrgan = organUid;
                break;
            }
        }
    }

    private void OnMapInit(EntityUid uid, ADTSlimeBodyComponent component, MapInitEvent args)
    {
        InitializeSlimeBody(uid, component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTSlimeBodyComponent, BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var slime, out var bloodstream))
        {
            if (_timing.CurTime < slime.NextUpdate)
                continue;

            slime.NextUpdate += TimeSpan.FromSeconds(1);

            // Extra blood regeneration so slime people don't slowly die from bloodloss.
            if (_timing.CurTime >= slime.BloodRegenNextUpdate)
            {
                slime.BloodRegenNextUpdate = _timing.CurTime + BloodRegenInterval;
                if (_bloodstream.GetBloodLevel((uid, bloodstream)) < bloodstream.BloodlossThreshold)
                    _bloodstream.TryRegulateBloodLevel(uid, FixedPoint2.New(BloodRegenPerTick));
            }

            if (slime.StomachOrgan is not { } stomachOrgan ||
                !_solutionContainer.TryGetSolution(stomachOrgan, "stomach", out var stomachSolution, out var stomach))
                continue;

            var amount = FixedPoint2.Min(FixedPoint2.New(TransferPerSecond), stomach.GetTotalPrototypeQuantity(slime.Reagent));
            if (amount <= FixedPoint2.Zero)
                continue;

            if (!_solutionContainer.TryGetSolution(uid, bloodstream.BloodSolutionName, out var bloodSolution, out _))
                continue;

            _solutionContainer.RemoveReagent(stomachSolution.Value, slime.Reagent, amount);
            _solutionContainer.TryAddReagent(bloodSolution.Value, slime.Reagent, amount);
        }
    }
}
