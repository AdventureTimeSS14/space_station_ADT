using Robust.Client.Audio;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;

namespace Content.Client.BoomBox
{
    public sealed class BoomBoxClientSystem : EntitySystem
    {

        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            _cfg.OnValueChanged(ADTCCVars.BoomBoxVolume, OnVolumeChanged, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _cfg.UnsubValueChanged(ADTCCVars.BoomBoxVolume, OnVolumeChanged);
        }

        private void OnVolumeChanged(float volume)
        {

        }
    }
}