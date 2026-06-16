using Content.Shared.ADT.Implants.KvasImplant;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Implants.KvasImplant;

public sealed class KvasImplantSystem : EntitySystem
{
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KvasImplantComponent, ActivateKvasImplantActionEvent>(OnActivate);
    }

    private void OnActivate(Entity<KvasImplantComponent> ent, ref ActivateKvasImplantActionEvent args)
    {
        if (args.Handled)
            return;

        var user = args.Performer;

        if (!_container.TryGetContainer(user, BodyComponent.ContainerID, out var organContainer))
            return;

        foreach (var organ in organContainer.ContainedEntities)
        {
            if (!TryComp<StomachComponent>(organ, out _))
                continue;

            var solution = new Solution();
            solution.AddReagent(ent.Comp.Reagent, ent.Comp.Amount);

            if (!_stomach.TryTransferSolution(organ, solution))
            {
                _popup.PopupEntity(Loc.GetString("kvas-implant-full"), user, user, PopupType.SmallCaution);
                return;
            }

            args.Handled = true;
            _audio.PlayPvs(ent.Comp.Sound, user);
            _popup.PopupEntity(Loc.GetString("kvas-implant-activate"), user, user, PopupType.Small);
            return;
        }
    }
}
