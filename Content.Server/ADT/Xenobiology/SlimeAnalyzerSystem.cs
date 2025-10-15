using System.Linq;
using Content.Shared.ADT.Xenobiology.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.ADT.Xenobiology.UI;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Server.PowerCell;
using Robust.Shared.Timing;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;

namespace Content.Server.Medical;

public sealed class SlimeAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SlimeAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SlimeAnalyzerComponent, SlimeAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SlimeAnalyzerComponent, EntGotInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<SlimeAnalyzerComponent, DroppedEvent>(OnDropped);
    }

    public override void Update(float frameTime)
    {
        var analyzerQuery = EntityQueryEnumerator<SlimeAnalyzerComponent, TransformComponent>();
        while (analyzerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if (component.NextUpdate > _gameTiming.CurTime)
                continue;

            if (component.ScannedEntity is not { } target)
                continue;

            if (Deleted(target))
            {
                StopAnalyzingEntity((uid, component));
                continue;
            }

            component.NextUpdate = _gameTiming.CurTime + component.UpdateInterval;

            var targetCoordinates = Transform(target).Coordinates;
            if (!_transform.InRange(targetCoordinates, transform.Coordinates, component.MaxScanRange))
            {
                StopAnalyzingEntity((uid, component));
                continue;
            }

            UpdateScannedUser(uid, target);
        }
    }

    private void OnAfterInteract(Entity<SlimeAnalyzerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<SlimeComponent>(args.Target) || !_cell.HasDrawCharge(ent, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningBeginSound, ent);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ScanDelay, new SlimeAnalyzerDoAfterEvent(), ent, target: args.Target, used: ent)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || doAfterCancelled)
            return;

        var msg = Loc.GetString("slime-analyzer-popup-scan-target", ("user", Identity.Entity(args.User, EntityManager)));
        _popup.PopupEntity(msg, args.Target.Value, args.Target.Value);
    }

    private void OnDoAfter(Entity<SlimeAnalyzerComponent> ent, ref SlimeAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null || !_cell.HasDrawCharge(ent, user: args.User))
            return;

        _audio.PlayPvs(ent.Comp.ScanningEndSound, ent);

        OpenUserInterface(args.User, ent);
        BeginAnalyzingEntity(ent, args.Target.Value);
        args.Handled = true;
    }

    private void OnInsertedIntoContainer(Entity<SlimeAnalyzerComponent> analyzer, ref EntGotInsertedIntoContainerMessage args)
    {
        if (analyzer.Comp.ScannedEntity is { } target)
            _toggle.TryDeactivate(analyzer.Owner);
    }

    private void OnDropped(Entity<SlimeAnalyzerComponent> analyzer, ref DroppedEvent args)
    {
        if (analyzer.Comp.ScannedEntity is { } target)
            _toggle.TryDeactivate(analyzer.Owner);
    }

    private void OpenUserInterface(EntityUid user, EntityUid analyzer)
    {
        if (!_uiSystem.HasUi(analyzer, SlimeAnalyzerUiKey.Key))
            return;

        _uiSystem.OpenUi(analyzer, SlimeAnalyzerUiKey.Key, user);
    }

    private void BeginAnalyzingEntity(Entity<SlimeAnalyzerComponent> analyzer, EntityUid target)
    {
        analyzer.Comp.ScannedEntity = target;

        _toggle.TryActivate(analyzer.Owner);

        UpdateScannedUser(analyzer, target);
    }

    private void StopAnalyzingEntity(Entity<SlimeAnalyzerComponent> analyzer)
    {
        analyzer.Comp.ScannedEntity = null;

        _toggle.TryDeactivate(analyzer.Owner);

        if (_uiSystem.HasUi(analyzer.Owner, SlimeAnalyzerUiKey.Key))
        {
            var actors = _uiSystem.GetActors(analyzer.Owner, SlimeAnalyzerUiKey.Key);
            foreach (var actor in actors)
            {
                _uiSystem.CloseUi(analyzer.Owner, SlimeAnalyzerUiKey.Key, actor);
            }
        }
    }

    public void UpdateScannedUser(EntityUid analyzer, EntityUid target)
    {
        if (!_uiSystem.HasUi(analyzer, SlimeAnalyzerUiKey.Key))
            return;

        if (!TryComp<MobGrowthComponent>(target, out var growth))
            return;

        if (!TryComp<SlimeComponent>(target, out var slime))
            return;

        if (!TryComp<HungerComponent>(target, out var hunger))
            return;

        var list = new List<string>(slime.PotentialMutations.Select(x => x.ToString()));

        var state = new SlimeAnalyzerScannedUserMessage(
            GetNetEntity(target),
            _hunger.GetHunger(hunger),
            hunger.Thresholds[HungerThreshold.Overfed],
            growth.CurrentStage,
            slime.Breed,
            slime.MutationChance,
            slime.SpecialMutationChance,
            list
        );

        _uiSystem.ServerSendUiMessage(analyzer, SlimeAnalyzerUiKey.Key, state);
    }
}
