using Content.Shared.ADT.Rituals;
using Content.Shared.ADT.UI;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.Rituals;

public sealed class ADTRitualSummonSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRitualObjectComponent, ADTRitualSummonSelectMessage>(OnSelected);
    }

    public void OpenPicker(EntityUid ritualObject, EntityUid shaman, List<EntityUid> candidates)
    {
        var entries = new List<ADTEntityPickerEntry>();

        foreach (var candidate in candidates)
        {
            var proto = MetaData(candidate).EntityPrototype?.ID;
            entries.Add(new ADTEntityPickerEntry(GetNetEntity(candidate), Name(candidate), proto));
        }

        _ui.SetUiState(ritualObject, ADTRitualSummonUiKey.Key, new ADTRitualSummonBuiState(entries));
        _ui.TryOpenUi(ritualObject, ADTRitualSummonUiKey.Key, shaman);
    }

    private void OnSelected(Entity<ADTRitualObjectComponent> ent, ref ADTRitualSummonSelectMessage args)
    {
        var target = GetEntity(args.Target);

        if (Deleted(target))
            return;

        _transform.SetCoordinates(target, Transform(ent.Owner).Coordinates);
    }
}
