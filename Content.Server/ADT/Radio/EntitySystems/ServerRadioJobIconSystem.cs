using Content.Server.VoiceMask;
using Content.Shared.Access.Components;
using Content.Shared.ADT.Radio;
using Content.Shared.ADT.Radio.Components;
using Content.Shared.ADT.Radio.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.StatusIcon;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Radio.EntitySystems;
public sealed class ServerRadioJobIconSystem : SharedRadioJobIconSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardComponent, EntInsertedIntoContainerMessage>(OnIdCardInserted);
        SubscribeLocalEvent<IdCardComponent, EntRemovedFromContainerMessage>(OnIdCardRemoved);
    }

    private void OnIdCardInserted(EntityUid uid, IdCardComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateOwnerRadioCache(args.Container.Owner);
    }

    private void OnIdCardRemoved(EntityUid uid, IdCardComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateOwnerRadioCache(args.Container.Owner);
    }

    private void UpdateOwnerRadioCache(EntityUid entity)
    {
        if (TryComp<InventoryComponent>(entity, out var _))
            UpdateRadioJobIcon(entity);
    }

    protected override (string iconId, string jobName) GetJobIcon(EntityUid entity)
    {
        if (TryGetActiveVoiceMaskJobIcon(entity, out var maskIconId, out var maskJobName))
        {
            return (maskIconId, maskJobName);
        }

        var (iconId, jobName) = base.GetJobIcon(entity);
        if (iconId != "JobIconNoId")
        {
            return (iconId, jobName);
        }

        if (TryComp<RadioJobIconOverrideComponent>(entity, out var overrideComp))
        {
            iconId = overrideComp.IconId;
            jobName = overrideComp.JobNameOverride
                ?? (Prototype.TryIndex<JobIconPrototype>(iconId, out var proto)
                    ? proto.LocalizedJobName
                    : string.Empty);
            return (iconId, jobName);
        }

        return (iconId, jobName);
    }

    private bool TryGetActiveVoiceMaskJobIcon(EntityUid entity, out string iconId, out string jobName)
    {
        iconId = string.Empty;
        jobName = string.Empty;

        if (!TryComp<InventoryComponent>(entity, out var _))
            return false;

        var enumerator = InventorySystem.GetSlotEnumerator(entity);
        while (enumerator.NextItem(out var item, out var slot))
        {
            if (slot == null || !Exists(item) || Deleted(item) || Terminating(item))
                continue;

            if (TryComp<VoiceMaskComponent>(item, out var voiceMask) && voiceMask.VoiceMaskJobIcon.HasValue)
            {
                iconId = voiceMask.VoiceMaskJobIcon.Value;
                jobName = Prototype.TryIndex<JobIconPrototype>(iconId, out var proto)
                    ? proto.LocalizedJobName
                    : Loc.GetString("voice-mask-job-icon-none");
                return true;
            }
        }

        return false;
    }

    public void UpdateWearerRadioJobIcon(EntityUid voiceMaskUid)
    {
        base.UpdateWearerRadioJobIcon(voiceMaskUid, _container);
    }
}
