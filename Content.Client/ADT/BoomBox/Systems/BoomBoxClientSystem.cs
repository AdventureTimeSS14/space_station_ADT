using Robust.Client.Audio;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.BoomBox;
using Robust.Shared.Audio;

namespace Content.Client.BoomBox;

public sealed class BoomBoxClientSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;

    private EntityUid? Stream = null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<BoomBoxPlayMessage>(OnBoomBoxPlay);
        SubscribeNetworkEvent<BoomBoxStopClientMessage>(OnBoomBoxStop);
    }

    private void OnBoomBoxPlay(BoomBoxPlayMessage msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { Valid: true } uid)
            return;

        var localVolume = _cfg.GetCVar(ADTCCVars.BoomBoxVolume); // Локальный CVar
        var finalVolume = localVolume + msg.ServerVolume; // Итоговая громкость

        // Создаем новый поток и воспроизводим его
        Stream = _audioSystem.PlayPvs(msg.SoundPath, uid, AudioParams.Default
            .WithVolume(finalVolume)
            .WithRolloffFactor(1f) // Делаем звук тише с расстоянием
            .WithReferenceDistance(1)
            .WithLoop(true)
            .WithMaxDistance(50))?.Entity;
    }

    private void OnBoomBoxStop(BoomBoxStopClientMessage msg)
    {
        Stream = _audioSystem.Stop(Stream);
    }
}
