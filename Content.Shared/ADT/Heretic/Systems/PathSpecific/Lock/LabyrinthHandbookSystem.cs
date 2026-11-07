//

using Content.Shared.Examine;
using Content.Shared.Heretic.Components.PathSpecific.Lock;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

public sealed partial class LabyrinthHandbookSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<LabyrinthHandbookComponent, BeforeRangedInteractEvent>(OnBeforeInteract);
    }

    private void OnBeforeInteract(Entity<LabyrinthHandbookComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!_heretic.IsHereticOrGhoul(args.User))
            return;

        if (!_examine.InRangeUnOccluded(args.User, args.ClickLocation))
            return;

        args.Handled = true;

        if (!TryComp(ent, out LabyrinthHandbookChargesComponent? charges))
            return;

        if (_timing.CurTime < charges.NextCharge)
        {
            _popup.PopupClient(Loc.GetString("heretic-ability-fail-cooldown"), args.User, args.User);
            return;
        }

        var wall = PredictedSpawnAtPosition(charges.WallProto, args.ClickLocation);
        _transform.SetWorldRotation(wall, Angle.Zero);

        charges.Charges--;
        if (charges.Charges <= 0)
        {
            charges.Charges = charges.MaxCharges;
            charges.NextCharge = _timing.CurTime + charges.RegenTime;
        }
        Dirty(ent, charges);
    }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class LabyrinthHandbookChargesComponent : Component
{
    [DataField]
    public int Charges = 5;

    [DataField]
    public int MaxCharges = 5;

    [DataField]
    public TimeSpan RegenTime = TimeSpan.FromSeconds(30);

    [DataField]
    public TimeSpan NextCharge;

    [DataField]
    public EntProtoId WallProto = "WallLabyrinth";
}
