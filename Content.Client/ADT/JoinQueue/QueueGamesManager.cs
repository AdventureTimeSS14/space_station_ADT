using Content.Shared.ADT.JoinQueue;
using Content.Shared.CCVar;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client.ADT.JoinQueue;

public sealed partial class QueueGamesManager
{
    private const string SpinSound = "/Audio/ADT/Machines/SlotMachine/slotmachine_spin.ogg";
    private const string SlotWinSound = "/Audio/ADT/Machines/SlotMachine/slotmachine_jackpotwin.ogg";
    private const string SlotLoseSound = "/Audio/Machines/buzz-two.ogg";
    private const string MatchWinSound = "/Audio/Effects/Arcade/win.ogg";
    private const string MatchLoseSound = "/Audio/Effects/Arcade/gameover.ogg";

    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IAudioManager _audio = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private IAudioSource? _source;

    public MsgQueueGameState? State { get; private set; }

    public event Action? StateUpdated;
    public event Action<bool>? SlotResult;

    public void Initialize()
    {
        _net.RegisterNetMessage<MsgQueueGameAction>();
        _net.RegisterNetMessage<MsgQueueGameState>(OnState);
        _net.RegisterNetMessage<MsgQueueSlotResult>(OnSlotResult);

        _net.Disconnect += (_, _) => State = null;
    }

    public void SendAction(QueueGameAction action, byte cell = 0)
    {
        _net.ClientSendMessage(new MsgQueueGameAction { Action = action, Cell = cell });
    }

    private void OnState(MsgQueueGameState msg)
    {
        var old = State;
        State = msg;

        if (msg.Spinning && old is not { Spinning: true })
            PlaySound(SpinSound);
        else if (msg.LastResult != old?.LastResult && old?.InMatch == true)
        {
            if (msg.LastResult == QueueGameResult.Win)
                PlaySound(MatchWinSound);
            else if (msg.LastResult == QueueGameResult.Lose)
                PlaySound(MatchLoseSound);
        }

        StateUpdated?.Invoke();
    }

    private void OnSlotResult(MsgQueueSlotResult msg)
    {
        PlaySound(msg.Won ? SlotWinSound : SlotLoseSound);
        SlotResult?.Invoke(msg.Won);
    }

    private void PlaySound(string path)
    {
        _source?.Dispose();
        _source = _audio.CreateAudioSource(_cache.GetResource<AudioResource>(path));

        if (_source == null)
            return;

        _source.Global = true;
        _source.Gain = _cfg.GetCVar(CCVars.InterfaceVolume);
        _source.StartPlaying();
    }
}
