using System.Linq;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Nutrition.Components;
using Content.Shared.Temperature;
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Surgery;

public sealed class SurgeryToolQualitySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SurgeryToolComponent, ExaminedEvent>(OnToolExamined);
    }

    private void OnToolExamined(Entity<SurgeryToolComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var proper = GetProperQualities(ent.Owner);

        if (ent.Comp.ActiveQualities is not { } active)
        {
            var qualities = ent.Comp.ExtraQualities.Union(proper).ToList();
            if (qualities.Count > 0)
                args.PushMarkup(Loc.GetString("surgery-tool-examine-qualities", ("qualities", FormatQualities(qualities))));
            return;
        }

        var activated = _toggle.IsActivated(ent.Owner);
        var current = activated ? active : ent.Comp.ExtraQualities;
        var other = activated ? ent.Comp.ExtraQualities : active;

        args.PushMarkup(Loc.GetString("surgery-tool-examine-current", ("qualities", FormatQualities(current))));
        args.PushMarkup(Loc.GetString("surgery-tool-examine-switch", ("qualities", FormatQualities(other))));
    }

    public List<ProtoId<SurgeryToolQualityPrototype>> GetToolQualities(EntityUid item, SurgeryToolComponent tool)
    {
        if (tool.ActiveQualities is { } active && _toggle.IsActivated(item))
            return active;

        return tool.ExtraQualities;
    }

    public List<ProtoId<SurgeryToolQualityPrototype>> GetProperQualities(EntityUid item)
    {
        var result = new List<ProtoId<SurgeryToolQualityPrototype>>();

        foreach (var quality in _proto.EnumeratePrototypes<SurgeryToolQualityPrototype>())
        {
            foreach (var entry in quality.Entries)
            {
                if (entry.Success < 1f || !EntryMatches(entry, item))
                    continue;

                result.Add(quality.ID);
                break;
            }
        }

        return result;
    }

    public string FormatQualities(List<ProtoId<SurgeryToolQualityPrototype>> qualities)
    {
        var names = new List<string>();
        foreach (var id in qualities)
        {
            if (_proto.TryIndex(id, out var quality))
                names.Add(Loc.GetString(quality.Name));
        }

        return names.Count > 0 ? string.Join(", ", names) : Loc.GetString("surgery-tool-examine-none");
    }

    public bool EntryMatches(SurgeryToolQualityEntry entry, EntityUid item)
    {
        if (!entry.HasCriteria)
            return false;

        if (entry.Whitelist != null && !_whitelist.IsWhitelistPass(entry.Whitelist, item))
            return false;

        if (entry.ToolQuality is { } toolQuality && !_tool.HasQuality(item, toolQuality))
            return false;

        if (entry.Utensil != UtensilType.None
            && (!TryComp<UtensilComponent>(item, out var utensil) || (utensil.Types & entry.Utensil) == 0))
            return false;

        if (entry.RequireHot)
        {
            var hot = new IsHotEvent();
            RaiseLocalEvent(item, hot);
            if (!hot.IsHot)
                return false;
        }

        return true;
    }
}
