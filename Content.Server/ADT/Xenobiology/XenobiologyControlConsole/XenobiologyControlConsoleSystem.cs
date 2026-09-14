using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.NPC.HTN;
using Content.Server.Speech.Components;
using Content.Shared.ADT.EyeControl;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;
using Content.Shared.DeviceLinking;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyControlConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<XenobiologyControlConsoleComponent, ExaminedEvent>(OnConsoleExamined);

        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyCaptureSlimeEvent>(OnCaptureSlime);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyPlaceSlimeEvent>(OnPlaceSlime);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyFeedMonkeyEvent>(OnFeedMonkey);
        SubscribeLocalEvent<EyeControlPilotComponent, XenobiologyRecycleMonkeyEvent>(OnRecycleMonkey);
    }

    private void OnMapInit(Entity<XenobiologyControlConsoleComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, XenobiologyControlConsoleComponent.SlimeContainerId);
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
        if (!args.IsInDetailsRange || !TryComp<StorageComponent>(ent, out var storage))
            return;

        args.PushMarkup(Loc.GetString("xenobiology-control-console-examine-cubes",
            ("count", storage.Container.ContainedEntities.Count)));
    }
}
