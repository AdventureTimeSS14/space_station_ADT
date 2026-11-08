using System.Linq;
using System.Numerics;
using System.Text;
using Content.Server.ADT.LogicCircuit;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.ADT.Lavaland.Helios;
using Content.Shared.TapeRecorder.Components;
using Content.Shared.Camera;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Light;
using Content.Shared.DeviceNetwork;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Trigger;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland.Helios;

public sealed class ADTHeliosMechanicsSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const string ReactPort = "ADTHeliosReact";
    private static readonly string[] Letters = { "А", "Б", "В", "Г" };
    private static readonly TimeSpan PlateInterval = TimeSpan.FromSeconds(0.25);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTHeliosSecretTextComponent, MapInitEvent>(OnSecretTextInit);
        SubscribeLocalEvent<ADTHeliosSecretTextComponent, ExaminedEvent>(OnSecretExamined);
        SubscribeLocalEvent<ADTHeliosCodeLockComponent, ComponentInit>(OnCodeLockInit);
        SubscribeLocalEvent<ADTHeliosCodeLockComponent, SignalReceivedEvent>(OnCodeLockSignal);
        SubscribeLocalEvent<ADTHeliosBreakerPanelComponent, ComponentInit>(OnBreakerInit);
        SubscribeLocalEvent<ADTHeliosBreakerPanelComponent, SignalReceivedEvent>(OnBreakerSignal);
        SubscribeLocalEvent<ADTHeliosDualKeyComponent, ComponentInit>(OnDualKeyInit);
        SubscribeLocalEvent<ADTHeliosDualKeyComponent, ActivateInWorldEvent>(OnDualKeyActivate);
        SubscribeLocalEvent<ADTHeliosHoldPlateComponent, ComponentInit>(OnPlateInit);
        SubscribeLocalEvent<ADTHeliosHoldPlateComponent, SignalReceivedEvent>(OnPlateSignal);
        SubscribeLocalEvent<ADTHeliosReactionComponent, ComponentInit>(OnReactionInit);
        SubscribeLocalEvent<ADTHeliosReactionComponent, SignalReceivedEvent>(OnReactionSignal);
        SubscribeLocalEvent<ADTHeliosReactionComponent, TriggerEvent>(OnReactionTrigger);
        SubscribeLocalEvent<ADTHeliosReactionComponent, StepTriggerAttemptEvent>(OnReactionStepAttempt);
        SubscribeLocalEvent<ADTHeliosRandomSpotComponent, MapInitEvent>(OnRandomSpotInit);
    }

    private readonly Queue<EntityUid> _pendingSpots = new();

    private void OnRandomSpotInit(Entity<ADTHeliosRandomSpotComponent> ent, ref MapInitEvent args)
    {
        _pendingSpots.Enqueue(ent);
    }

    private void ResolveSpots(EntityUid mapUid)
    {
        var done = EnsureComp<ADTHeliosRandomSpotsDoneComponent>(mapUid);
        var groups = new Dictionary<string, List<Entity<ADTHeliosRandomSpotComponent>>>();
        var query = EntityQueryEnumerator<ADTHeliosRandomSpotComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spot, out var xform))
        {
            if (xform.MapUid != mapUid || done.Groups.Contains(spot.Group))
                continue;

            if (!groups.TryGetValue(spot.Group, out var list))
                groups[spot.Group] = list = new List<Entity<ADTHeliosRandomSpotComponent>>();
            list.Add((uid, spot));
        }

        foreach (var (group, spots) in groups.OrderBy(g => g.Value.Count).ThenBy(g => g.Key))
        {
            done.Groups.Add(group);
            var free = spots.Where(s => !done.UsedTiles.Contains(SpotTile(s))).ToList();
            var chosen = _random.Pick(free.Count > 0 ? free : spots);
            done.UsedTiles.Add(SpotTile(chosen));
            Spawn(chosen.Comp.Proto, Transform(chosen).Coordinates);
            foreach (var spot in spots)
                Del(spot);
        }
    }

    private (EntityUid, Vector2i) SpotTile(EntityUid uid)
    {
        var xform = Transform(uid);
        var pos = xform.LocalPosition;
        return (xform.GridUid ?? EntityUid.Invalid, new Vector2i((int) MathF.Floor(pos.X), (int) MathF.Floor(pos.Y)));
    }

    public Dictionary<string, string> GetSecrets(EntityUid onMap)
    {
        var map = Transform(onMap).MapUid ?? onMap;
        var comp = EnsureComp<ADTHeliosSecretsComponent>(map);
        if (comp.Values.Count == 0)
            Generate(comp.Values);

        return comp.Values;
    }

    private void Generate(Dictionary<string, string> s)
    {
        var years = Enumerable.Range(2551, 19).Where(y => !y.ToString().Contains('0')).ToList();
        var year = _random.Pick(years).ToString();
        s["YEAR"] = year;

        if (_random.Prob(0.5f))
        {
            s["WAREHOUSE_CODE"] = new string(year.Reverse().ToArray());
            s["WAREHOUSE_RULE"] = "год закладки комплекса, записанный задом наперёд";
            s["WAREHOUSE_RULE_SHORT"] = "году закладки, записанному наоборот";
        }
        else
        {
            s["WAREHOUSE_CODE"] = year[2..] + year[..2];
            s["WAREHOUSE_RULE"] = "год закладки комплекса, у которого две последние цифры переставили в начало";
            s["WAREHOUSE_RULE_SHORT"] = "году закладки, у которого последние две цифры переставили вперёд";
        }

        s["MCB_ID"] = Digits(4);
        s["MCB_CODE"] = s["MCB_ID"];

        s["VORN_ID"] = Digits(4);
        s["VORN_ID_HEAD"] = s["VORN_ID"][..2];
        s["VORN_CODE"] = s["VORN_ID"];

        var hour = _random.Pick(new[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 23 });
        var minute = _random.Next(1, 6) * 10 + _random.Next(1, 10);
        s["JOULE_TIME"] = $"{hour}:{minute}";
        s["JOULE_CODE"] = $"{hour}{minute}";

        var bad = _random.Next(4);
        var on = Enumerable.Range(0, 4).Where(i => i != bad).ToList();
        s["BRK_BAD"] = Letters[bad];
        s["BRK_MASK"] = string.Concat(Enumerable.Range(0, 4).Select(i => i == bad ? '0' : '1'));
        s["BRK_ON"] = $"{Letters[on[0]]}, {Letters[on[1]]} и {Letters[on[2]]}";
        s["BRK_FIRST"] = Letters[on[0]];
        s["BRK_PAIR_A"] = Letters[on[1]];
        s["BRK_PAIR_B"] = Letters[on[2]];
    }

    private string Digits(int count)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < count; i++)
            sb.Append(_random.Next(1, 10));

        return sb.ToString();
    }

    public string Substitute(EntityUid onMap, string text)
    {
        if (!text.Contains('{'))
            return text;

        foreach (var (key, value) in GetSecrets(onMap))
            text = text.Replace("{" + key + "}", value);

        return text;
    }

    private void OnSecretTextInit(Entity<ADTHeliosSecretTextComponent> ent, ref MapInitEvent args)
    {
        if (TryComp<PaperComponent>(ent, out var paper) && paper.Content.Contains('{'))
            _paper.SetContent((ent, paper), Substitute(ent, paper.Content), false);

        if (TryComp<TapeCassetteComponent>(ent, out var tape))
        {
            foreach (var message in tape.RecordedData)
                message.Message = Substitute(ent, message.Message);
        }
    }

    private void OnSecretExamined(Entity<ADTHeliosSecretTextComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.ExamineText is { } text)
            args.PushMarkup(Substitute(ent, text));
    }

    private void OnCodeLockInit(Entity<ADTHeliosCodeLockComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, Enumerable.Range(1, 9).Select(i => (Robust.Shared.Prototypes.ProtoId<Content.Shared.DeviceLinking.SinkPortPrototype>) $"ADTLogicIn{i}").ToArray());
        _deviceLink.EnsureSourcePorts(ent, "ADTLogicOut1", "ADTLogicOut2", "ADTLogicOut3");
    }

    private void OnCodeLockSignal(Entity<ADTHeliosCodeLockComponent> ent, ref SignalReceivedEvent args)
    {
        if (ent.Comp.Unlocked || !args.Port.StartsWith("ADTLogicIn") || !int.TryParse(args.Port["ADTLogicIn".Length..], out var digit))
            return;

        TypeDigit(ent, digit);
    }

    public bool TypeDigit(Entity<ADTHeliosCodeLockComponent> ent, int digit)
    {
        if (ent.Comp.Unlocked || digit is < 1 or > 9)
            return ent.Comp.Unlocked;

        var code = GetSecrets(ent).GetValueOrDefault(ent.Comp.SecretKey, "????");
        var typed = ent.Comp.Typed + digit;
        if (typed.Length > code.Length)
            typed = typed[^code.Length..];
        ent.Comp.Typed = typed;

        if (typed == code)
        {
            ent.Comp.Unlocked = true;
            _deviceLink.InvokePort(ent, "ADTLogicOut1");
            SendScreen(ent, "ДОСТУП", "#33ff66");
            return true;
        }

        SendScreen(ent, "КОД:" + typed, "#ffb000");
        return false;
    }

    private void SendScreen(EntityUid uid, string text, string color)
    {
        _deviceLink.InvokePort(uid, "ADTLogicOut2", new NetworkPayload { [ADTLogicCircuitSystem.LogicValueKey] = text });
        _deviceLink.InvokePort(uid, "ADTLogicOut3", new NetworkPayload { [ADTLogicCircuitSystem.LogicValueKey] = color });
    }

    private void OnBreakerInit(Entity<ADTHeliosBreakerPanelComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, "ADTLogicIn1", "ADTLogicIn2", "ADTLogicIn3", "ADTLogicIn4");
        _deviceLink.EnsureSourcePorts(ent, "ADTLogicOut1", "ADTLogicOut2", "ADTLogicOut3");
    }

    private void OnBreakerSignal(Entity<ADTHeliosBreakerPanelComponent> ent, ref SignalReceivedEvent args)
    {
        if (!args.Port.StartsWith("ADTLogicIn") || !int.TryParse(args.Port["ADTLogicIn".Length..], out var line) || line is < 1 or > 4)
            return;

        var state = SignalState.Momentary;
        if (args.Data != null && args.Data.TryGetValue(DeviceNetworkConstants.LogicState, out SignalState received))
            state = received;

        ent.Comp.Lines[line - 1] = state switch
        {
            SignalState.High => true,
            SignalState.Low => false,
            _ => !ent.Comp.Lines[line - 1],
        };

        UpdateBreakers(ent);
    }

    private void UpdateBreakers(Entity<ADTHeliosBreakerPanelComponent> ent)
    {
        var mask = GetSecrets(ent).GetValueOrDefault(ent.Comp.SecretKey, "1111");
        var overload = false;
        var ok = true;
        for (var i = 0; i < 4; i++)
        {
            var need = mask[i] == '1';
            if (!need && ent.Comp.Lines[i])
                overload = true;
            if (need != ent.Comp.Lines[i])
                ok = false;
        }

        if (overload)
            SendScreen(ent, "ПЕРЕГРУЗ", "#ff3333");
        else if (ok)
            SendScreen(ent, "ПИТАНИЕ", "#33ff66");
        else
            SendScreen(ent, "НЕТ ПИТ.", "#ffb000");

        if (ok && !ent.Comp.Powered)
            _deviceLink.InvokePort(ent, "ADTLogicOut1");
        ent.Comp.Powered = ok;
    }

    private void OnDualKeyInit(Entity<ADTHeliosDualKeyComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent, ent.Comp.SolvedPort);
    }

    private void OnDualKeyActivate(Entity<ADTHeliosDualKeyComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        TurnKey(ent, args.User);
    }

    public void TurnKey(Entity<ADTHeliosDualKeyComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Finished)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("helios-dualkey-done"), ent, user.Value);
            return;
        }

        var now = _timing.CurTime;
        ent.Comp.TurnedAt = now;
        _audio.PlayPvs(ent.Comp.TurnSound, ent);
        _appearance.SetData(ent, ADTHeliosPuzzleVisuals.Lit, true);

        var group = new List<Entity<ADTHeliosDualKeyComponent>>();
        var query = EntityQueryEnumerator<ADTHeliosDualKeyComponent>();
        while (query.MoveNext(out var uid, out var key))
        {
            if (key.Group == ent.Comp.Group && Transform(uid).MapID == Transform(ent).MapID)
                group.Add((uid, key));
        }

        if (group.All(k => k.Comp.TurnedAt is { } t && now - t <= k.Comp.Window))
        {
            foreach (var key in group)
            {
                key.Comp.Finished = true;
                _deviceLink.InvokePort(key, key.Comp.SolvedPort);
            }

            _audio.PlayPvs(ent.Comp.SolveSound, ent);
            _popup.PopupEntity(Loc.GetString("helios-dualkey-solved"), ent, PopupType.Medium);
            return;
        }

        if (user != null)
            _popup.PopupEntity(Loc.GetString("helios-dualkey-waiting"), ent, user.Value);
    }

    private void OnPlateInit(Entity<ADTHeliosHoldPlateComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(ent, ent.Comp.HeldPort, ent.Comp.ReleasedPort);
        _deviceLink.EnsureSinkPorts(ent, ent.Comp.LockPort);
    }

    private void OnPlateSignal(Entity<ADTHeliosHoldPlateComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port != ent.Comp.LockPort.Id || ent.Comp.Locked)
            return;

        ent.Comp.Locked = true;
        _appearance.SetData(ent, ADTHeliosPuzzleVisuals.Lit, true);
        _deviceLink.InvokePort(ent, ent.Comp.HeldPort);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        while (_pendingSpots.TryDequeue(out var spotUid))
        {
            if (!TerminatingOrDeleted(spotUid) && HasComp<ADTHeliosRandomSpotComponent>(spotUid))
                ResolveSpots(Transform(spotUid).MapUid ?? spotUid);
        }

        var now = _timing.CurTime;

        var keys = EntityQueryEnumerator<ADTHeliosDualKeyComponent>();
        while (keys.MoveNext(out var uid, out var key))
        {
            if (!key.Finished && key.TurnedAt is { } t && now - t > key.Window)
            {
                key.TurnedAt = null;
                _appearance.SetData(uid, ADTHeliosPuzzleVisuals.Lit, false);
            }
        }

        var plates = EntityQueryEnumerator<ADTHeliosHoldPlateComponent, TransformComponent>();
        while (plates.MoveNext(out var uid, out var plate, out var xform))
        {
            if (plate.Locked || now < plate.NextCheck)
                continue;

            plate.NextCheck = now + PlateInterval;
            var held = false;
            foreach (var other in _lookup.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(uid, xform), 0.45f))
            {
                if (_mobState.IsAlive(other, other.Comp) && HasComp<HumanoidProfileComponent>(other))
                {
                    held = true;
                    break;
                }
            }

            if (held == plate.Held)
                continue;

            plate.Held = held;
            _appearance.SetData(uid, ADTHeliosPuzzleVisuals.Lit, held);
            _audio.PlayPvs(plate.PressSound, uid);
            _deviceLink.InvokePort(uid, held ? plate.HeldPort : plate.ReleasedPort);
        }
    }

    private void OnReactionInit(Entity<ADTHeliosReactionComponent> ent, ref ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(ent, ReactPort);
    }

    private void OnReactionSignal(Entity<ADTHeliosReactionComponent> ent, ref SignalReceivedEvent args)
    {
        if (args.Port == ReactPort)
            React(ent);
    }

    private void OnReactionStepAttempt(Entity<ADTHeliosReactionComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnReactionTrigger(Entity<ADTHeliosReactionComponent> ent, ref TriggerEvent args)
    {
        React(ent);
    }

    public void React(Entity<ADTHeliosReactionComponent> ent)
    {
        if (ent.Comp.Once && ent.Comp.Fired)
            return;

        ent.Comp.Fired = true;
        var coords = _transform.GetMapCoordinates(ent);

        if (ent.Comp.Sound != null)
            _audio.PlayPvs(ent.Comp.Sound, ent);

        if (ent.Comp.Flicker)
        {
            foreach (var light in _lookup.GetEntitiesInRange<PoweredLightComponent>(coords, ent.Comp.Radius))
            {
                var blink = EnsureComp<BlinkingPoweredLightComponent>(light);
                blink.StopBlinkingTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(2.5f, 5f));
                Dirty(light, blink);
                _appearance.SetData(light, PoweredLightVisuals.Blinking, true);
            }
        }

        if (ent.Comp.Shake > 0)
        {
            foreach (var cam in _lookup.GetEntitiesInRange<CameraRecoilComponent>(coords, ent.Comp.Radius))
            {
                var s = ent.Comp.Shake;
                _recoil.KickCamera(cam, new Vector2(_random.NextFloat(-s, s), _random.NextFloat(-s, s)), cam.Comp);
            }
        }

        foreach (var proto in ent.Comp.Spawns)
            Spawn(proto, Transform(ent).Coordinates);
    }
}
