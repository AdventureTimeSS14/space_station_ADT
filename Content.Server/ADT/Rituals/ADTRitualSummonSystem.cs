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
        SubscribeLocalEvent<ADTRitualSummonPickerComponent, BoundUIClosedEvent>(OnClosed);
    }

    public void OpenPicker(EntityUid ritualObject, EntityUid shaman, List<EntityUid> candidates)
    {
        var entries = new List<ADTEntityPickerEntry>();
        var picker = EnsureComp<ADTRitualSummonPickerComponent>(ritualObject);

        picker.Shaman = shaman;
        picker.Candidates.Clear();

        foreach (var candidate in candidates)
        {
            var proto = MetaData(candidate).EntityPrototype?.ID;
            entries.Add(new ADTEntityPickerEntry(GetNetEntity(candidate), Name(candidate), proto));
            picker.Candidates.Add(candidate);
        }

        _ui.SetUiState(ritualObject, ADTRitualSummonUiKey.Key, new ADTRitualSummonBuiState(entries));

        if (!_ui.TryOpenUi(ritualObject, ADTRitualSummonUiKey.Key, shaman))
            RemComp<ADTRitualSummonPickerComponent>(ritualObject);
    }

    private void OnClosed(Entity<ADTRitualSummonPickerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is ADTRitualSummonUiKey)
            RemComp<ADTRitualSummonPickerComponent>(ent.Owner);
    }

    private void OnSelected(Entity<ADTRitualObjectComponent> ent, ref ADTRitualSummonSelectMessage args)
    {
        if (!TryComp<ADTRitualSummonPickerComponent>(ent.Owner, out var picker))
            return;

        if (args.Actor != picker.Shaman)
            return;

        var target = GetEntity(args.Target);

        if (!picker.Candidates.Contains(target) || Deleted(target))
            return;

        RemComp<ADTRitualSummonPickerComponent>(ent.Owner);
        _ui.CloseUi(ent.Owner, ADTRitualSummonUiKey.Key, picker.Shaman);

        _transform.SetCoordinates(target, Transform(ent.Owner).Coordinates);
    }
}
