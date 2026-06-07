using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.TTS;
using Content.Shared.Corvax.TTS;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Client.Corvax.TTS;

public sealed partial class TTSSystem
{
    [Dependency] private readonly IDependencyCollection _collection = default!;

    private void OnPlayRadioTTS(PlayRadioTTSEvent ev)
    {
        if (_cfg.GetCVar(ADTCCVars.ReplaceTTSWithBarks) == true)
            return;

        if (HasComp<DeafTraitComponent>(_playerManager.LocalEntity))
            return;

        _sawmill.Verbose($"Play radio TTS audio {ev.Data.Length}");

        var filePath = new ResPath($"{_fileIdx++}.ogg");

        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player != null)
        {
            if (_language.CanUnderstand(player.Value, ev.LanguageProtoId))
                _contentRoot.AddOrUpdateFile(filePath, ev.Data);
            else
                _contentRoot.AddOrUpdateFile(filePath, ev.LanguageData);
        }
        else
            _contentRoot.AddOrUpdateFile(filePath, ev.Data);

        var audioResource = new AudioResource();
        audioResource.Load(_collection, Prefix / filePath);

        var soundSpecifier = new ResolvedPathSpecifier(Prefix / filePath);

        _audio.PlayGlobal(audioResource.AudioStream, soundSpecifier);

        _contentRoot.RemoveFile(filePath);
    }
}
