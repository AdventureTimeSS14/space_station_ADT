using System.Linq;
using Content.Server.Botany.Components;
using Content.Server.Popups;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Swab;

namespace Content.Server.Botany.Systems;

public sealed class BotanySwabSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MutationSystem _mutationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BotanySwabComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BotanySwabComponent, BotanySwabDoAfterEvent>(OnDoAfter);
    }

    // ADT-Tweak-Start
    /// <summary>
    /// If swab was used.
    /// </summary>
    private bool IsUsed(BotanySwabComponent swab)
    {
        return swab.SeedData != null || swab.AllergicTriggers != null;
    }
    // ADT-Tweak-End

    /// <summary>
    /// This handles swab examination text
    /// so you can tell if they are used or not.
    /// </summary>
    private void OnExamined(EntityUid uid, BotanySwabComponent swab, ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            if (IsUsed(swab)) // ADT-Tweak: IsUsed
                args.PushMarkup(Loc.GetString("swab-used"));
            else
                args.PushMarkup(Loc.GetString("swab-unused"));
        }
    }

    /// <summary>
    /// Handles swabbing a plant.
    /// </summary>
    private void OnAfterInteract(EntityUid uid, BotanySwabComponent swab, AfterInteractEvent args)
    {
        // ADT-Tweak-Start
        bool isTargetPlant = HasComp<PlantHolderComponent>(args.Target);
        bool isTargetBody = HasComp<BodyComponent>(args.Target);
        // ADT-Tweak-End

        if (args.Target == null || !args.CanReach || !isTargetPlant && !isTargetBody) // ADT-Tweak
            return;

        // ADT-Tweak-Start
        if (isTargetPlant && swab.AllergicTriggers != null)
        {
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-unusable-plant"), args.User, args.User);
            return;
        }

        if (isTargetBody && swab.SeedData != null)
        {
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-unusable-bio"), args.User, args.User);
            return;
        }
        // ADT-Tweak-End

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.SwabDelay, new BotanySwabDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    // ADT-Tweak-Start
    /// <summary>
    /// Save seed data or cross-pollenate.
    /// </summary>
    private void OnPlantDoAfter(EntityUid uid, BotanySwabComponent swab, PlantHolderComponent plant, DoAfterEvent args)
    {
        if (swab.SeedData == null)
        {
            // Pick up pollen
            swab.SeedData = plant.Seed;
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-from"), args.Args.Target!.Value, args.Args.User);
        }
        else
        {
            var old = plant.Seed;
            if (old == null)
                return;
            plant.Seed = _mutationSystem.Cross(swab.SeedData, old); // Cross-pollenate
            swab.SeedData = old; // Transfer old plant pollen to swab
            _popupSystem.PopupEntity(Loc.GetString("botany-swab-to"), args.Args.Target!.Value, args.Args.User);
        }
    }
    // ADT-Tweak-End

    // ADT-Tweak-Start
    /// <summary>
    /// Save allergy triggers if exists.
    /// </summary>
    private void OnCreatureDoAfter(EntityUid uid, BotanySwabComponent swab, DoAfterEvent args)
    {
        if (TryComp<AllergicComponent>(args.Args.Target!.Value, out var allergic))
            if (swab.AllergicTriggers == null)
                swab.AllergicTriggers = allergic.Triggers;
            else
                /** Snowball unique allergies on swab */
                swab.AllergicTriggers = swab.AllergicTriggers.Concat(allergic.Triggers).Distinct().ToList();

        /** Mark as used anyway */
        if (swab.AllergicTriggers == null)
            swab.AllergicTriggers = new();

        _popupSystem.PopupEntity(Loc.GetString("botany-swab-used-bio"), args.Args.User, args.Args.User);
    }
    // ADT-Tweak-End

    private void OnDoAfter(EntityUid uid, BotanySwabComponent swab, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        // ADT-Tweak-Start
        if (TryComp<PlantHolderComponent>(args.Args.Target, out var plant))
            OnPlantDoAfter(uid, swab, plant, args);
        else if (HasComp<BodyComponent>(args.Args.Target))
            OnCreatureDoAfter(uid, swab, args);
        else
            return;
        // ADT-Tweak-End

        args.Handled = true;
    }
}

