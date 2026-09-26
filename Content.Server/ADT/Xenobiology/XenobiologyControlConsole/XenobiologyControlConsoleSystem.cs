using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.NPC.HTN;
using Content.Server.Speech.Components;
using Content.Shared.ADT.EyeControl;
using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;
using Content.Shared.Chat;
using Content.Shared.Construction;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Xenobiology.XenobiologyControlConsole;

public sealed class XenobiologyControlConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyControlConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, ExaminedEvent>(OnConsoleExamined);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, MachineDeconstructedEvent>(OnMachineDeconstructed);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, EntInsertedIntoContainerMessage>(OnConsoleContainerChanged);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, EntRemovedFromContainerMessage>(OnConsoleContainerChanged);

        SubscribeLocalEvent<EyeControlEyeComponent, ComponentStartup>(OnEyeStartup);
        SubscribeLocalEvent<EyeControlPilotComponent, ComponentShutdown>(OnPilotShutdown);

        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyCaptureSlimeEvent>(OnCaptureSlime);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyPlaceSlimeEvent>(OnPlaceSlime);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyFeedMonkeyEvent>(OnFeedMonkey);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyRecycleMonkeyEvent>(OnRecycleMonkey);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyAnalyzeSlimeEvent>(OnAnalyzeSlime);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyApplyMutationPotionEvent>(OnApplyMutationPotion);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyApplyStabilizerPotionEvent>(OnApplyStabilizerPotion);
    }

    private void OnAfterInteractUsing(Entity<XenobiologyControlConsoleComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (_tags.HasTag(args.Used, ent.Comp.MonkeyCubeTag))
        {
            if (!_storage.Insert(ent, args.Used, out _))
                return;

            args.Handled = true;
            RefreshPilotView(ent);
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-monkey-cube-inserted",
                ("count", CountMonkeyCubes(ent))), ent, args.User);
            return;
        }

        if (HasComp<SlimeMutationPotionComponent>(args.Used))
        {
            ent.Comp.MutationPotions += 1;
            QueueDel(args.Used);
            RefreshPilotView(ent);
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-mutation-potion-inserted",
                ("count", ent.Comp.MutationPotions)), ent, args.User);
            args.Handled = true;
            return;
        }

        if (HasComp<SlimeStabilizerPotionComponent>(args.Used))
        {
            ent.Comp.StabilizerPotions += 1;
            QueueDel(args.Used);
            RefreshPilotView(ent);
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-stabilizer-potion-inserted",
                ("count", ent.Comp.StabilizerPotions)), ent, args.User);
            args.Handled = true;
        }
    }

    private void OnMachineDeconstructed(Entity<XenobiologyControlConsoleComponent> ent, ref MachineDeconstructedEvent args)
    {
        for (var i = 0; i < ent.Comp.MutationPotions; i++)
            SpawnNextToOrDrop("SlimeMutationPotion", ent.Owner);
        for (var i = 0; i < ent.Comp.StabilizerPotions; i++)
            SpawnNextToOrDrop("SlimeStabilizerPotion", ent.Owner);
    }

    private void OnMapInit(Entity<XenobiologyControlConsoleComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, XenobiologyControlConsoleComponent.SlimeContainerId);
    }

    private void OnEyeStartup(Entity<EyeControlEyeComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Pilot is { } pilot && TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            UpdateView((ent.Comp.Console, console), pilot);
    }

    private void OnPilotShutdown(Entity<EyeControlPilotComponent> ent, ref ComponentShutdown args)
    {
        RemComp<XenobiologyConsoleViewComponent>(ent.Owner);
    }

    private void OnConsoleContainerChanged(Entity<XenobiologyControlConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
        => RefreshPilotView(ent);

    private void OnConsoleContainerChanged(Entity<XenobiologyControlConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
        => RefreshPilotView(ent);

    private void RefreshPilotView(Entity<XenobiologyControlConsoleComponent> ent)
    {
        if (TryComp<EyeControlConsoleComponent>(ent, out var eyeConsole) && eyeConsole.Pilot is { } pilot)
            UpdateView(ent, pilot);
    }

    private void UpdateView(Entity<XenobiologyControlConsoleComponent> ent, EntityUid pilot)
    {
        var view = EnsureComp<XenobiologyConsoleViewComponent>(pilot);
        var cubes = CountMonkeyCubes(ent);
        var slimes = _container.TryGetContainer(ent, XenobiologyControlConsoleComponent.SlimeContainerId, out var slimeContainer)
            ? slimeContainer.ContainedEntities.Count
            : 0;

        if (view.MaxStoredSlimes == ent.Comp.MaxSlimeCapacity
            && view.MonkeyCubes == cubes
            && view.StoredSlimes == slimes
            && view.MutationPotions == ent.Comp.MutationPotions
            && view.StabilizerPotions == ent.Comp.StabilizerPotions)
            return;

        view.MaxStoredSlimes = ent.Comp.MaxSlimeCapacity;
        view.MonkeyCubes = cubes;
        view.StoredSlimes = slimes;
        view.MutationPotions = ent.Comp.MutationPotions;
        view.StabilizerPotions = ent.Comp.StabilizerPotions;
        Dirty(pilot, view);
    }

    private int CountMonkeyCubes(EntityUid console)
    {
        return TryComp<StorageComponent>(console, out var storage)
            ? storage.Container.ContainedEntities.Count
            : 0;
    }

    private bool TryGetSlimeNearEye(EntityUid eye, float range, out EntityUid slime)
    {
        foreach (var candidate in _lookup.GetEntitiesInRange<SlimeComponent>(Transform(eye).Coordinates, range))
        {
            slime = candidate.Owner;
            return true;
        }

        slime = EntityUid.Invalid;
        return false;
    }

    private void OnCaptureSlime(Entity<EyeControlPilotComponent> ent, ref XenobiologyCaptureSlimeEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console) ||
            !_container.TryGetContainer(ent.Comp.Console, XenobiologyControlConsoleComponent.SlimeContainerId, out var container))
            return;

        if (container.ContainedEntities.Count >= console.MaxSlimeCapacity)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-slime-storage-full"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        var target = _lookup.GetEntitiesInRange<SlimeComponent>(eyeCoords, console.InteractRange)
            .Select(e => e.Owner)
            .FirstOrDefault(candidate => !_mobState.IsDead(candidate) && !container.Contains(candidate));

        if (target == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-slime"), ent.Comp.Eye, ent);
            return;
        }

        if (TryComp<HTNComponent>(target, out var htn))
            _htn.SetHTNEnabled((target, htn), false);

        if (!_container.Insert(target, container))
            return;

        _audio.PlayPvs(console.SuctionSound, ent.Comp.Console);
        args.Handled = true;
    }

    private void OnPlaceSlime(Entity<EyeControlPilotComponent> ent, ref XenobiologyPlaceSlimeEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console) ||
            !_container.TryGetContainer(ent.Comp.Console, XenobiologyControlConsoleComponent.SlimeContainerId, out var container))
            return;

        var slime = container.ContainedEntities.FirstOrDefault();

        if (slime == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-slime-storage-empty"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        _container.Remove(slime, container);
        _xform.SetCoordinates(slime, eyeCoords);

        if (TryComp<HTNComponent>(slime, out var htn))
            _htn.SetHTNEnabled((slime, htn), true, 2f);

        _audio.PlayPvs(console.EjectSound, ent.Comp.Console);
        args.Handled = true;
    }

    private void OnFeedMonkey(Entity<EyeControlPilotComponent> ent, ref XenobiologyFeedMonkeyEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console) ||
            !TryComp<StorageComponent>(ent.Comp.Console, out var storage))
            return;

        var cube = storage.Container.ContainedEntities.FirstOrDefault();

        if (cube == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-monkey-stock"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        _container.Remove(cube, storage.Container);
        QueueDel(cube);
        Spawn("MobMonkey", eyeCoords);

        _audio.PlayPvs(console.EjectSound, ent.Comp.Console);
        args.Handled = true;
    }

    private void OnRecycleMonkey(Entity<EyeControlPilotComponent> ent, ref XenobiologyRecycleMonkeyEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            return;

        if (!TryGetLinkedRecycler(ent.Comp.Console, out _, out var recycler))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-recycler"), ent.Comp.Eye, ent);
            return;
        }

        var eyeCoords = Transform(ent.Comp.Eye).Coordinates;

        var target = _lookup.GetEntitiesInRange(eyeCoords, console.InteractRange)
            .FirstOrDefault(candidate => HasComp<MonkeyAccentComponent>(candidate) && _mobState.IsDead(candidate));

        if (target == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-dead-monkey"), ent.Comp.Eye, ent);
            return;
        }

        for (var i = 0; i < recycler.CubeProduction; i++)
        {
            var newCube = Spawn("MonkeyCube", Transform(ent.Comp.Console).Coordinates);

            if (!_storage.Insert(ent.Comp.Console, newCube, out _))
            {
                QueueDel(newCube);
                break;
            }
        }

        QueueDel(target);

        _audio.PlayPvs(console.SuctionSound, ent.Comp.Console);
        args.Handled = true;
    }

    private void OnAnalyzeSlime(Entity<EyeControlPilotComponent> ent, ref XenobiologyAnalyzeSlimeEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            return;

        if (!TryComp<ActorComponent>(args.Performer, out var actor))
            return;

        if (!TryGetSlimeNearEye(ent.Comp.Eye, console.InteractRange, out var target))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-slime"), ent.Comp.Eye, ent);
            return;
        }

        var slime = Comp<SlimeComponent>(target);
        var nutrition = TryComp<HungerComponent>(target, out var hunger)
            ? (int) _hunger.GetHunger(hunger)
            : 0;

        var breedName = _prototype.TryIndex(slime.Breed, out var breed)
            ? Loc.GetString(breed.BreedName)
            : slime.Breed.Id;

        var message = string.Join("\n",
            Loc.GetString("xenobiology-control-console-analyze-name", ("name", Name(target))),
            Loc.GetString("xenobiology-control-console-analyze-breed", ("breed", breedName)),
            Loc.GetString("xenobiology-control-console-analyze-nutrition", ("nutrition", nutrition)),
            Loc.GetString("xenobiology-control-console-analyze-chance", ("chance", (int) (slime.MutationChance * 100))),
            Loc.GetString("xenobiology-control-console-analyze-extracts", ("extracts", slime.ExtractsProduced + slime.SlimeSteroidAmount)));

        _chat.ChatMessageToOne(ChatChannel.Local, message, message, EntityUid.Invalid, false, actor.PlayerSession.Channel);
        args.Handled = true;
    }

    private void OnApplyMutationPotion(Entity<EyeControlPilotComponent> ent, ref XenobiologyApplyMutationPotionEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            return;

        if (console.MutationPotions < 1)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-mutation-potions"), ent.Comp.Eye, ent);
            return;
        }

        if (!TryGetSlimeNearEye(ent.Comp.Eye, console.InteractRange, out var target))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-slime"), ent.Comp.Eye, ent);
            return;
        }

        var slime = Comp<SlimeComponent>(target);
        if (slime.MutationChance >= 1f)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-mutation-max", ("name", Name(target))), ent.Comp.Eye, ent);
            return;
        }

        slime.MutationChance = Math.Clamp(slime.MutationChance + SlimeMutationPotionComponent.MutationChangeAmount, 0f, 1f);
        console.MutationPotions -= 1;
        RefreshPilotView((ent.Comp.Console, console));
        _popup.PopupEntity(Loc.GetString("xenobiology-control-console-mutation-potion-applied",
            ("name", Name(target)),
            ("chance", (int) (slime.MutationChance * 100))), ent.Comp.Eye, ent);
        args.Handled = true;
    }

    private void OnApplyStabilizerPotion(Entity<EyeControlPilotComponent> ent, ref XenobiologyApplyStabilizerPotionEvent args)
    {
        if (!TryComp<XenobiologyControlConsoleComponent>(ent.Comp.Console, out var console))
            return;

        if (console.StabilizerPotions < 1)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-stabilizer-potions"), ent.Comp.Eye, ent);
            return;
        }

        if (!TryGetSlimeNearEye(ent.Comp.Eye, console.InteractRange, out var target))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-no-slime"), ent.Comp.Eye, ent);
            return;
        }

        var slime = Comp<SlimeComponent>(target);
        if (slime.MutationChance <= 0f)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-control-console-stabilizer-min", ("name", Name(target))), ent.Comp.Eye, ent);
            return;
        }

        slime.MutationChance = Math.Clamp(slime.MutationChance + SlimeStabilizerPotionComponent.MutationChangeAmount, 0f, 1f);
        console.StabilizerPotions -= 1;
        RefreshPilotView((ent.Comp.Console, console));
        _popup.PopupEntity(Loc.GetString("xenobiology-control-console-stabilizer-potion-applied",
            ("name", Name(target)),
            ("chance", (int) (slime.MutationChance * 100))), ent.Comp.Eye, ent);
        args.Handled = true;
    }

    private bool TryGetLinkedRecycler(EntityUid console, out EntityUid recycler, [NotNullWhen(true)] out XenobiologyMonkeyRecyclerComponent? recyclerComp)
    {
        var query = EntityQueryEnumerator<XenobiologyMonkeyRecyclerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_deviceLink.GetLinks(uid, console).Count > 0)
            {
                recycler = uid;
                recyclerComp = comp;
                return true;
            }
        }

        recycler = default;
        recyclerComp = null;
        return false;
    }

    private void OnConsoleExamined(Entity<XenobiologyControlConsoleComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryComp<StorageComponent>(ent, out var storage))
        {
            args.PushMarkup(Loc.GetString("xenobiology-control-console-examine-cubes",
                ("count", storage.Container.ContainedEntities.Count)));
        }

        args.PushMarkup(Loc.GetString("xenobiology-control-console-examine-potions",
            ("mutation", ent.Comp.MutationPotions),
            ("stabilizer", ent.Comp.StabilizerPotions)));
    }
}
