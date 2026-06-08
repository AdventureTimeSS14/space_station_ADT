using Content.Shared.ADT.KvasImplant;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server.ADT.KvasImplant;

public sealed class KvasImplantSystem : EntitySystem
{
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string KvasReagent = "Kvass";
    private const float KvasAmount = 15f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActivateKvasImplantActionEvent>(OnActivate);
    }

    private void OnActivate(ActivateKvasImplantActionEvent args)
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
            solution.AddReagent(KvasReagent, KvasAmount);

            if (!_stomach.TryTransferSolution(organ, solution))
            {
                _popup.PopupEntity(Loc.GetString("kvas-implant-full"), user, user, PopupType.SmallCaution);
                return;
            }

            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("kvas-implant-activate"), user, user, PopupType.Small);
            return;
        }
    }
}
