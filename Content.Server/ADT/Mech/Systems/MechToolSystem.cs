using Content.Shared.ADT.Mech.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Server.Mech.EntitySystems;

public sealed class MechToolSystem : SharedMechToolSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechThrusterComponent, MapInitEvent>(OnArchimedesMapInit);
    }

    private void OnArchimedesMapInit(EntityUid uid, MechThrusterComponent comp, MapInitEvent args)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return;
        if (hands.Count > 0)
            return;

        var noItems = new EntityWhitelist { Components = new[] { "Item" } };
        _hands.AddHand((uid, hands), "mech_tool_hand", HandLocation.Middle, blacklist: noItems);
    }
}
