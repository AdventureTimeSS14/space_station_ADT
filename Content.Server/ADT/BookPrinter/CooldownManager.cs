using Robust.Shared.Configuration;
using Robust.Shared.Timing;
using Content.Shared.ADT.CCVar;

namespace Content.Server.ADT.BookPrinter
{
    public sealed class GlobalBookPrinterCooldownManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private TimeSpan? _lastUploadTime;

        public GlobalBookPrinterCooldownManager()
        {
            IoCManager.InjectDependencies(this);
        }

        public bool IsUploadAvailable()
        {
            if (!_cfg.GetCVar(ADTCCVars.BookPrinterUploadCooldownEnabled))
                return true;

            if (_lastUploadTime == null)
                return true;

            var cooldownDuration = TimeSpan.FromSeconds(_cfg.GetCVar(ADTCCVars.BookPrinterUploadCooldown));
            var timeSinceLastUpload = _timing.CurTime - _lastUploadTime.Value;

            return timeSinceLastUpload >= cooldownDuration;
        }

        public TimeSpan GetRemainingCooldown()
        {
            if (!_cfg.GetCVar(ADTCCVars.BookPrinterUploadCooldownEnabled) || _lastUploadTime == null)
                return TimeSpan.Zero;

            var cooldownDuration = TimeSpan.FromSeconds(_cfg.GetCVar(ADTCCVars.BookPrinterUploadCooldown));
            var timeSinceLastUpload = _timing.CurTime - _lastUploadTime.Value;
            var remainingTime = cooldownDuration - timeSinceLastUpload;

            return remainingTime > TimeSpan.Zero ? remainingTime : TimeSpan.Zero;
        }

        public void RegisterUpload()
        {
            _lastUploadTime = _timing.CurTime;
        }

        public TimeSpan GetCooldownDuration()
        {
            return TimeSpan.FromSeconds(_cfg.GetCVar(ADTCCVars.BookPrinterUploadCooldown));
        }

        public bool IsCooldownEnabled()
        {
            return _cfg.GetCVar(ADTCCVars.BookPrinterUploadCooldownEnabled);
        }

        public void ResetCooldown()
        {
            _lastUploadTime = null;
        }
    }
}
