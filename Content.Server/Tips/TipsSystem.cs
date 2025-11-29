using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Dataset;
using Content.Shared.Tips;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Server.Administration.Logs;
using Content.Shared.Database;

namespace Content.Server.Tips;

public sealed class TipsSystem : SharedTipsSystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    private bool _tipsEnabled;
    private float _tipTimeOutOfRound;
    private float _tipTimeInRound;
    private string _tipsDataset = "";
    private float _tipTippyChance;

    [ViewVariables(VVAccess.ReadWrite)]
    private TimeSpan _nextTipTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        Subs.CVar(_cfg, CCVars.TipsEnabled, SetEnabled, true);
        Subs.CVar(_cfg, CCVars.TipFrequencyOutOfRound, value => _tipTimeOutOfRound = value, true);
        Subs.CVar(_cfg, CCVars.TipFrequencyInRound, value => _tipTimeInRound = value, true);
        Subs.CVar(_cfg, CCVars.TipsDataset, value => _tipsDataset = value, true);
        Subs.CVar(_cfg, CCVars.TipsTippyChance, value => _tipTippyChance = value, true);

        RecalculateNextTipTime();
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        // reset for lobby -> inround
        // reset for inround -> post but not post -> lobby
        if (ev.New == GameRunLevel.InRound || ev.Old == GameRunLevel.InRound)
        {
            RecalculateNextTipTime();
        }
    }

    private void SetEnabled(bool value)
    {
        _tipsEnabled = value;

        if (_nextTipTime != TimeSpan.Zero)
            RecalculateNextTipTime();
    }

    public override void RecalculateNextTipTime()
    {
        if (_ticker.RunLevel == GameRunLevel.InRound)
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeInRound);
        }

        // ADT-Tweak-Start Логируем сообщение
        _adminLogger.Add(
            LogType.AdminMessage,
            LogImpact.Low,
            $"[АДМИНАБУЗ] {shell.Player?.Name} used the command tippy or tip. EntityPrototype: {args[2]}"
        );
        // ADT-Tweak-End
        ActorComponent? actor = null;
        if (args[0] != "all")
        {
            ICommonSession? session;
            if (args.Length > 0)
            {
                // Get player entity
                if (!_playerManager.TryGetSessionByUsername(args[0], out session))
                {
                    shell.WriteLine(Loc.GetString("cmd-tippy-error-no-user"));
                    return;
                }
            }
            else
            {
                session = shell.Player;
            }

            if (session?.AttachedEntity is not { } user)
            {
                shell.WriteLine(Loc.GetString("cmd-tippy-error-no-user"));
                return;
            }

            if (!TryComp(user, out actor))
            {
                shell.WriteError(Loc.GetString("cmd-tippy-error-no-user"));
                return;
            }
        }

        var ev = new TippyEvent(args[1]);

        if (args.Length > 2)
        {
            ev.Proto = args[2];
            if (!_prototype.HasIndex<EntityPrototype>(args[2]))
            {
                shell.WriteError(Loc.GetString("cmd-tippy-error-no-prototype", ("proto", args[2])));
                return;
            }
        }

        if (args.Length > 3)
            ev.SpeakTime = float.Parse(args[3]);
        else
        {
            _nextTipTime = _timing.CurTime + TimeSpan.FromSeconds(_tipTimeOutOfRound);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_tipsEnabled)
            return;

        if (_nextTipTime != TimeSpan.Zero && _timing.CurTime > _nextTipTime)
        {
            AnnounceRandomTip();
            RecalculateNextTipTime();
        }
    }

    public override void SendTippy(
        string message,
        EntProtoId? prototype = null,
        float speakTime = 5f,
        float slideTime = 3f,
        float waddleInterval = 0.5f)
    {
        var ev = new TippyEvent(message, prototype, speakTime, slideTime, waddleInterval);
        RaiseNetworkEvent(ev);
    }

    public override void SendTippy(
        ICommonSession session,
        string message,
        EntProtoId? prototype = null,
        float speakTime = 5f,
        float slideTime = 3f,
        float waddleInterval = 0.5f)
    {
        var ev = new TippyEvent(message, prototype, speakTime, slideTime, waddleInterval);
        RaiseNetworkEvent(ev, session);
    }

    public override void AnnounceRandomTip()
    {
        if (!_prototype.TryIndex<LocalizedDatasetPrototype>(_tipsDataset, out var tips))
            return;

        var tip = _random.Pick(tips.Values);
        var msg = Loc.GetString("tips-system-chat-message-wrap", ("tip", Loc.GetString(tip)));

        if (_random.Prob(_tipTippyChance))
        {
            var speakTime = GetSpeechTime(msg);
            SendTippy(msg, speakTime: speakTime);
        }
        else
        {
            _chat.ChatMessageToManyFiltered(
                Filter.Broadcast(),
                ChatChannel.OOC,
                tip,
                msg,
                EntityUid.Invalid,
                false,
                false,
                Color.MediumPurple);
        }
    }
}
