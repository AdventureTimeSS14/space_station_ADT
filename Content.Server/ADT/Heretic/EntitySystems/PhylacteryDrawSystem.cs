using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Heretic.EntitySystems;

[RegisterComponent]
public sealed partial class HereticPhylacteryComponent : Component
{
    [DataField]
    public string SolutionName = "phylactery";

    [DataField]
    public SoundSpecifier DrawSound = new SoundPathSpecifier("/Audio/Effects/Chemistry/bubbles.ogg");
}

public sealed class PhylacteryDrawSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private const float BloodDrawPercent = 0.15f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticPhylacteryComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<HereticPhylacteryComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (!TryComp<BloodstreamComponent>(args.Target.Value, out var bloodstream))
            return;

        var bloodLevel = _bloodstream.GetBloodLevel((args.Target.Value, bloodstream));
        if (bloodLevel <= 0f)
        {
            _popup.PopupClient(Loc.GetString("phylactery-no-blood"), args.User, args.User);
            args.Handled = true;
            return;
        }

        var normalVolume = bloodstream.BloodReferenceSolution.Volume;
        var currentBlood = FixedPoint2.New((float)normalVolume * bloodLevel);
        var drainAmount = FixedPoint2.New((float)currentBlood * BloodDrawPercent);

        if (drainAmount <= FixedPoint2.Zero)
            return;

        if (!TryComp<SolutionContainerManagerComponent>(ent, out var phylSolMan))
            return;

        if (!_solution.TryGetSolution((ent, phylSolMan), ent.Comp.SolutionName, out var phylSol, out var phylSolution))
            return;

        var availableSpace = phylSolution.AvailableVolume;
        if (availableSpace <= FixedPoint2.Zero)
        {
            _popup.PopupClient(Loc.GetString("phylactery-full"), args.User, args.User);
            args.Handled = true;
            return;
        }

        drainAmount = FixedPoint2.Min(drainAmount, availableSpace);

        _bloodstream.TryModifyBloodLevel((args.Target.Value, bloodstream), -drainAmount);

        foreach (var (reagentId, quantity) in bloodstream.BloodReferenceSolution.Contents)
        {
            var ratio = (float)drainAmount / (float)normalVolume;
            var reagentAmount = FixedPoint2.New((float)quantity * ratio);
            _solution.TryAddReagent(phylSol.Value, reagentId.Prototype, reagentAmount, out _);
        }

        _audio.PlayPvs(ent.Comp.DrawSound, ent);
        _popup.PopupClient(Loc.GetString("phylactery-draw", ("amount", drainAmount)), args.User, args.User);
        args.Handled = true;
    }
}
