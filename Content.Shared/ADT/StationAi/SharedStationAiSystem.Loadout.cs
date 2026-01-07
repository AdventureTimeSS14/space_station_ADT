using Content.Shared.Roles;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    public static string ExtraLoadoutScreenId = "ai-screen";
    public static string ExtraLoadoutLawsetId = "ai-lawset";
    public static string ExtraLoadoutNameId = "ai-name";

    private void InitializeLoadout()
    {
        SubscribeLocalEvent<StationAiCustomizationComponent, ApplyLoadoutExtrasEvent>(ApplyExtras);
        SubscribeLocalEvent<StationAiBrainComponent, EntGotInsertedIntoContainerMessage>(OnBrainInsert);
        SubscribeLocalEvent<StationAiCoreComponent, ApplyLoadoutExtrasEvent>(ApplyCoreExtras);
    }

    private void ApplyExtras(Entity<StationAiCustomizationComponent> ent, ref ApplyLoadoutExtrasEvent args)
    {
        SetLoadoutExtraLawset(ent, args.Data);

        if (args.Data.TryGetValue(ExtraLoadoutScreenId, out var screen))
            ent.Comp.ProtoIds[_stationAiCoreCustomGroupProtoId] = screen;

        if (args.Data.TryGetValue(ExtraLoadoutNameId, out var name))
            _metadata.SetEntityName(ent, name);

    }

    private void OnBrainInsert(Entity<StationAiBrainComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != StationAiHolderComponent.Container)
            return;

        _appearance.SetData(args.Container.Owner, StationAiCustomVisualState.Key, ent.Comp.AiScreenProto);
    }

    private void ApplyCoreExtras(Entity<StationAiCoreComponent> ent, ref ApplyLoadoutExtrasEvent args)
    {
        SetLoadoutExtraVisuals(ent, args.Data);
    }

    public void SetLoadoutExtraVisuals(EntityUid ent, Dictionary<string, string> data)
    {
        var screen = "Default";
        if (data.TryGetValue(ExtraLoadoutScreenId, out var customScreen))
            screen = customScreen;

        if (TryComp<StationAiCustomizationComponent>(ent, out var custom))
        {
            custom.ProtoIds[_stationAiCoreCustomGroupProtoId] = screen;
        }
        _appearance.SetData(ent, StationAiCustomVisualState.Key, screen);

        if (!data.TryGetValue(ExtraLoadoutNameId, out var name) || name == string.Empty)
            return;

        if (!_containers.TryGetContainer(ent, StationAiHolderComponent.Container, out var container) ||
            container.Count == 0)
            return;

        var brain = container.ContainedEntities[0];

        _metadata.SetEntityName(ent, name);
        _metadata.SetEntityName(brain, name);
    }

    public virtual void SetLoadoutExtraLawset(EntityUid brain, Dictionary<string, string> data)
    {
    }

    public virtual void SetLoadoutOnTakeover(EntityUid core, EntityUid brain)
    {
    }
}
