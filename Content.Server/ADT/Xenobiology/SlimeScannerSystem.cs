using Robust.Server.GameObjects;
using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.Components.Equipment;
using Content.Shared.ADT.Xenobiology.Systems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;
using System.Linq;

namespace Content.Server.ADT.Xenobiology;

public sealed partial class SlimeScannerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prot = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly XenobiologySystem _xenobio = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeComponent, AfterInteractUsingEvent>(OnSlimeAfterInteractUsing);
        SubscribeLocalEvent<SlimeExtractComponent, AfterInteractUsingEvent>(OnExtractAfterInteractUsing);
        SubscribeLocalEvent<SlimeScannerComponent, SlimeScannerDoAfterEvent>(OnDoAfter);
    }

    private void OnSlimeAfterInteractUsing(Entity<SlimeComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!CanInteract(args))
            return;

        StartScanDoAfter(args.User, args.Used, ent);
        args.Handled = true;
    }

    private void OnExtractAfterInteractUsing(Entity<SlimeExtractComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!CanInteract(args))
            return;

        StartScanDoAfter(args.User, args.Used, ent);
        args.Handled = true;
    }

    private void OnDoAfter(Entity<SlimeScannerComponent> ent, ref SlimeScannerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        _audioSystem.PlayPvs(ent.Comp.ScanningEndSound, ent.Owner);

        OpenScannerUI(args.User, ent.Owner, args.Target.Value);
        args.Handled = true;
    }

    private bool CanInteract(AfterInteractUsingEvent args)
        => !args.Handled && args.Target != null && args.CanReach && HasComp<SlimeScannerComponent>(args.Used);

    private void StartScanDoAfter(EntityUid user, EntityUid scanner, EntityUid target)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(1), new SlimeScannerDoAfterEvent(), scanner, target: target, used: scanner)
        {
            NeedHand = true,
            BreakOnMove = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterArgs);
    }

    private void OpenScannerUI(EntityUid user, EntityUid scanner, EntityUid target)
    {
        if (!_uiSystem.HasUi(scanner, SlimeScannerUiKey.Key))
            return;

        _uiSystem.OpenUi(scanner, SlimeScannerUiKey.Key, user);

        var msg = BuildScannedMessage(target);
        if (msg != null)
            _uiSystem.ServerSendUiMessage(scanner, SlimeScannerUiKey.Key, msg);
    }

    private SlimeScannerScannedMessage? BuildScannedMessage(EntityUid target)
    {
        if (TryComp<SlimeComponent>(target, out var slime))
        {
            var breed = _prot.Index<BreedPrototype>(slime.Breed);

            var breedName = Loc.GetString(breed.BreedName);

            var mutations = slime.PotentialMutations
                .Select(id => Loc.GetString(_prot.Index<BreedPrototype>(id).BreedName))
                .ToList();

            var slimeEnt = new Entity<SlimeComponent>(target, slime);
            var density = _xenobio.GetLocalSlimeDensity(slimeEnt);
            var slowdown = _xenobio.GetBreedingSlowdown(slimeEnt);
            var maxSlimes = _cfg.GetCVar(SimpleStationCCVars.XenobiologyMaxSlimesPerGrid);

            return new SlimeScannerScannedMessage(
                GetNetEntity(target),
                breedName,
                slime.SlimeColor.ToHex(),
                slime.MutationChance,
                mutations,
                slime.ExtractsProduced,
                null,
                density,
                maxSlimes,
                slowdown);
        }

        if (TryComp<SlimeExtractComponent>(target, out var _) &&
            TryComp<ReactiveComponent>(target, out var reactive) &&
            reactive.Reactions != null)
        {
            var reagents = new List<ExtractReagentInfo>();
            foreach (var reaction in reactive.Reactions)
            {
                if (reaction.Reagents == null)
                    continue;

                foreach (var reagentId in reaction.Reagents)
                {
                    if (_prot.TryIndex<ReagentPrototype>(reagentId, out var reagentProto))
                    {
                        reagents.Add(new ExtractReagentInfo(
                            reagentProto.ID,
                            reagentProto.SubstanceColor.ToHex()));
                    }
                }
            }

            return new SlimeScannerScannedMessage(
                GetNetEntity(target),
                null, null, null, null, null,
                reagents);
        }

        return null;
    }
}
