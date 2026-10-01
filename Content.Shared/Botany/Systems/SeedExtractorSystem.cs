using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Traits.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;
// ADT-Tweak-Start
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
// ADT-Tweak-End

namespace Content.Shared.Botany.Systems;

public sealed partial class SeedExtractorSystem : EntitySystem
{
    [Dependency] private BotanySystem _botany = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;

    [Dependency] private EntityQuery<ProduceComponent> _produceQuery = default!;

    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<SeedExtractorComponent> ent, ref InteractUsingEvent args)
    {
        if (!_powerReceiver.IsPowered(ent.Owner))
            return;

        if (!_produceQuery.TryComp(args.Used, out var produce))
            return;

        if (produce.PlantProtoId == null)
            return;

        EntityUid? snapshot = null;
        if (produce.PlantData != null)
            snapshot = produce.PlantData;

        if (_botany.TryGetPlantComponent<PlantTraitSeedlessComponent>(snapshot, produce.PlantProtoId, out _))
        {
            _popup.PopupCursor(Loc.GetString("seed-extractor-component-no-seeds", ("name", args.Used)),
                args.User,
                PopupType.MediumCaution);
            return;
        }

        _popup.PopupCursor(Loc.GetString("seed-extractor-component-interact-message", ("name", args.Used)),
            args.User,
            PopupType.Medium);

        PredictedQueueDel(args.Used);
        args.Handled = true;


        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        var amount = (int)MathF.Round(random.NextFloat(ent.Comp.BaseSeeds.Min, ent.Comp.BaseSeeds.Max + 1) * ent.Comp.SeedMultiplier); // ADT-Tweak
        var coords = Transform(ent).Coordinates;

        for (var i = 0; i < amount; i++)
        {
            if (_botany.TryGetPlantComponent<PlantDataComponent>(snapshot, produce.PlantProtoId, out var plantData))
                _botany.SpawnSeedPacket(plantData, produce.PlantProtoId.Value, snapshot, coords, args.User);
        }
    }

    // ADT-Tweak-Start
    [SubscribeLocalEvent]
    private void OnPartsRefresh(Entity<SeedExtractorComponent> ent, ref RefreshPartsEvent args)
    {
        ent.Comp.SeedMultiplier = args.GetStatMultiplier(MachineStat.Speed);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private static void OnUpgradeExamine(Entity<SeedExtractorComponent> ent, ref UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-seed-extraction", ent.Comp.SeedMultiplier, benefit: true);
    }
    // ADT-Tweak-End
}
