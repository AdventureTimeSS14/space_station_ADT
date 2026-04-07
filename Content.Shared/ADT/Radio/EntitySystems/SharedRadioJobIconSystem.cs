using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ADT.Radio.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Radio.EntitySystems;
public abstract class SharedRadioJobIconSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager Prototype = default!;
    [Dependency] protected readonly AccessReaderSystem AccessReader = default!;
    [Dependency] protected readonly InventorySystem InventorySystem = default!;

    protected EntityQuery<RadioJobIconComponent> RadioJobIconQuery => _radioJobIconQuery;
    private EntityQuery<RadioJobIconComponent> _radioJobIconQuery;

    public override void Initialize()
    {
        base.Initialize();

        _radioJobIconQuery = GetEntityQuery<RadioJobIconComponent>();
        SubscribeLocalEvent<RadioJobIconComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RadioJobIconComponent component, ref MapInitEvent args)
    {
        if (!Exists(uid) || Deleted(uid) || Terminating(uid))
            return;

        var (iconId, jobName) = GetJobIcon(uid);

        if (component.JobIconId.Id != iconId || component.JobName != jobName)
        {
            component.JobIconId = new ProtoId<JobIconPrototype>(iconId);
            component.JobName = jobName;
            Dirty(uid, component);

            var ev = new RadioJobIconUpdatedEvent(iconId, jobName);
            RaiseLocalEvent(uid, ref ev);
        }
    }
    public void UpdateRadioJobIcon(EntityUid uid)
    {
        if (!Exists(uid) || Deleted(uid) || Terminating(uid))
            return;

        if (!_radioJobIconQuery.HasComp(uid))
            EnsureComp<RadioJobIconComponent>(uid);

        var component = _radioJobIconQuery.GetComponent(uid);
        var (iconId, jobName) = GetJobIcon(uid);

        if (component.JobIconId.Id != iconId || component.JobName != jobName)
        {
            component.JobIconId = new ProtoId<JobIconPrototype>(iconId);
            component.JobName = jobName;
            Dirty(uid, component);

            var ev = new RadioJobIconUpdatedEvent(iconId, jobName);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    protected virtual (string iconId, string jobName) GetJobIcon(EntityUid entity)
    {
        var iconId = "JobIconNoId";
        string? jobName = null;

        if (!Exists(entity) || Deleted(entity) || Terminating(entity))
            return (iconId, string.Empty);

        if (AccessReader.FindAccessItemsInventory(entity, out var items))
        {
            foreach (var item in items)
            {
                if (!Exists(item) || Deleted(item) || Terminating(item))
                    continue;

                if (TryComp<IdCardComponent>(item, out var idCard))
                {
                    iconId = idCard.JobIcon;
                    jobName = idCard.LocalizedJobTitle;
                    break;
                }

                if (TryComp<PdaComponent>(item, out var pda) && pda.ContainedId.HasValue)
                {
                    var containedId = pda.ContainedId.Value;
                    if (Exists(containedId) && !Deleted(containedId) && !Terminating(containedId)
                        && TryComp<IdCardComponent>(containedId, out var pdaIdCard))
                    {
                        iconId = pdaIdCard.JobIcon;
                        jobName = pdaIdCard.LocalizedJobTitle;
                        break;
                    }
                }
            }
        }

        return (iconId, jobName ?? string.Empty);
    }

    public (string iconId, string jobName) GetJobIconPublic(EntityUid entity)
    {
        return GetJobIcon(entity);
    }

    public void UpdateWearerRadioJobIcon(EntityUid itemUid, SharedContainerSystem containerSystem)
    {
        if (!Exists(itemUid) || Deleted(itemUid) || Terminating(itemUid))
            return;

        if (containerSystem.TryGetContainingContainer(itemUid, out var container))
        {
            if (!RadioJobIconQuery.HasComp(container.Owner))
                EnsureComp<RadioJobIconComponent>(container.Owner);

            UpdateRadioJobIcon(container.Owner);
        }
    }
}
