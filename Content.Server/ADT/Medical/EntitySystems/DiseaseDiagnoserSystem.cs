using System.Linq;
using System.Text;
using Content.Server.Botany;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Medical;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Medical.EntitySystems;

public sealed class DiseaseDiagnoserSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseDiagnoserComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, ContainerIsRemovingAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<DiseaseDiagnoserComponent, PowerChangedEvent>(OnPowerChanged);
    }

    #region Events

    private void OnActivateInWorld(Entity<DiseaseDiagnoserComponent> entity, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        TryStart(entity, args.User);
        args.Handled = true;
    }

    private void OnRemoveAttempt(Entity<DiseaseDiagnoserComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        if (args.Container.ID == ent.Comp.ContainerId && ent.Comp.Working)
            args.Cancel();
    }

    private void OnPowerChanged(Entity<DiseaseDiagnoserComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            Stop(ent);
    }

    #endregion

    #region Lifecycle

    public void TryStart(Entity<DiseaseDiagnoserComponent> entity, EntityUid? user)
    {
        var (uid, comp) = entity;
        if (comp.Working)
            return;

        if (!HasPower(entity))
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-no-power"), entity, user.Value);
            return;
        }

        if (!_container.TryGetContainer(uid, comp.ContainerId, out var container) || container.Count == 0)
        {
            if (user != null)
                _popup.PopupClient(Loc.GetString("solution-container-mixer-popup-nothing-to-mix"), entity, user.Value);
            return;
        }

        comp.Working = true;
        comp.WorkingSoundEntity = _audio.PlayPvs(comp.WorkingSound, entity, comp.WorkingSound?.Params.WithLoop(true));
        comp.WorkTimeEnd = _timing.CurTime + comp.WorkDuration;
        _appearance.SetData(entity, DiseaseDiagnoserVisuals.Printing, true);
        Dirty(uid, comp);
    }

    public void Stop(Entity<DiseaseDiagnoserComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Working)
            return;
        _audio.Stop(comp.WorkingSoundEntity);
        _appearance.SetData(entity, DiseaseDiagnoserVisuals.Printing, false);
        comp.Working = false;
        comp.WorkingSoundEntity = null;
        Dirty(uid, comp);
    }

    public string GetReport(BotanySwabComponent swabComponent)
    {
        StringBuilder builder = new();

        if (swabComponent.AllergicTriggers == null || swabComponent.AllergicTriggers.Count() == 0)
            builder.Append(Loc.GetString("paper-allergy-prefix", ("allergies", Loc.GetString("paper-allergy-no"))));
        else
        {
            List<string> localizedNames = new();
            foreach (ProtoId<ReagentPrototype> trigger in swabComponent.AllergicTriggers)
            {
                localizedNames.Add(_proto.Index<ReagentPrototype>(trigger).LocalizedName.ToLower());
            }

            builder.Append(Loc.GetString("paper-allergy-prefix", ("allergies", String.Join(", ", localizedNames))));
        }

        return builder.ToString();
    }

    public void Finish(Entity<DiseaseDiagnoserComponent> entity)
    {
        var (uid, comp) = entity;
        if (!comp.Working)
            return;
        Stop(entity);

        if (!TryComp<DiseaseDiagnoserComponent>(entity, out var reactionMixer)
            || !_container.TryGetContainer(uid, comp.ContainerId, out var container))
            return;

        if (container.ContainedEntities.Count == 0)
            return;

        var swabEnt = container.ContainedEntities.First();
        if (!TryComp<BotanySwabComponent>(swabEnt, out var swabComponent))
            return;

        var printed = Spawn("DiagnosisReportPaper", Transform(uid).Coordinates);

        if (TryComp<PaperComponent>(printed, out var paper))
        {
            _paperSystem.SetContent((printed, paper), GetReport(swabComponent));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseDiagnoserComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Working)
                continue;

            if (_timing.CurTime < comp.WorkTimeEnd)
                continue;

            Finish((uid, comp));
        }
    }

    #endregion

    public bool HasPower(Entity<DiseaseDiagnoserComponent> entity)
    {
        return this.IsPowered(entity, EntityManager);
    }
}
