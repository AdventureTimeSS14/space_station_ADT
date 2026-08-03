using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Examine;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.ADT.Xenobiology;

namespace Content.Shared.ADT.Xenobiology.Systems;

public sealed partial class XenobiologySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly MobGrowthSystem _mobGrowth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeTaming();
        SubscribeBreeding();

        SubscribeLocalEvent<SlimeComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        UpdateMitosis();
    }

    private void OnExamined(Entity<SlimeComponent> slime, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || _net.IsClient)
            return;

        if (slime.Comp.Tamer == args.Examiner)
            args.PushMarkup(Loc.GetString("slime-examined-tamer"));

        if (slime.Comp.Stomach.Count > 0)
            args.PushMarkup(Loc.GetString("slime-examined-stomach"));
    }

    public EntProtoId GetProducedExtract(Entity<SlimeComponent> slime)
    {
        return _prototypeManager.TryIndex(slime.Comp.Breed, out var breedPrototype)
            ? breedPrototype.ProducedExtract
            : slime.Comp.DefaultExtract;
    }
}