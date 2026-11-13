using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;

namespace Content.Shared.ADT.AutoSurgeon;

public sealed class AutoSurgeonExamineSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoSurgeonComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<AutoSurgeonComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var empty = true;
        for (var i = 0; i < ent.Comp.ImplantSlots.Count; i++)
        {
            if (_itemSlots.GetItemOrNull(ent.Owner, AutoSurgeonComponent.SlotId + "_" + i) is not { } item)
                continue;

            empty = false;
            args.PushMarkup(Loc.GetString("autosurgeon-examine-loaded", ("item", item)));
        }

        if (empty)
            args.PushMarkup(Loc.GetString("autosurgeon-examine-empty"));

        if (ent.Comp.Uses >= 0)
            args.PushMarkup(Loc.GetString("autosurgeon-examine-uses", ("uses", ent.Comp.Uses)));
    }
}
