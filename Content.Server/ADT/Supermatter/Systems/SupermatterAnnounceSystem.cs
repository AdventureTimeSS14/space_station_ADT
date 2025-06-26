using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using System.Text;
using Robust.Shared.Player;
using Content.Server.Chat.Systems;
using Content.Server.Radio.EntitySystems;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Supermatter.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Robust.Shared.Timing;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.Supermatter.Systems;

public sealed partial class SupermatterSystem
{
    /// <summary>
    /// Handles core damage announcements
    /// </summary>
    public void AnnounceCoreDamage(EntityUid uid, SupermatterComponent sm)
    {
        if (sm.Damage == 0 || !sm.HasBeenPowered)
            return;

        string message;
        var global = false;

        var integrity = GetIntegrity(sm).ToString("0.00");

        if (sm.Delamming && !sm.DelamAnnounced)
        {
            var sb = new StringBuilder();
            var loc = sm.PreferredDelamType switch
            {
                DelamType.Cascade => "supermatter-delam-cascade",
                DelamType.Singularity => "supermatter-delam-singularity",
                DelamType.Tesla => "supermatter-delam-tesla",
                _ => "supermatter-delam-explosion"
            };

            sb.AppendLine(Loc.GetString(loc));
            sb.Append(Loc.GetString("supermatter-seconds-before-delam", ("seconds", sm.DelamTimer)));

            message = sb.ToString();
            global = true;
            sm.DelamAnnounced = true;
            sm.YellTimer = TimeSpan.FromSeconds(sm.DelamTimer / 2);

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        if (_timing.CurTime < sm.YellLast + sm.YellTimer)
            return;

        if (sm.Damage < sm.DamageDelaminationPoint && sm.DelamAnnounced)
        {
            message = Loc.GetString("supermatter-delam-cancel", ("integrity", integrity));
            sm.DelamAnnounced = false;
            sm.YellTimer = TimeSpan.FromSeconds(_config.GetCVar(ADTCCVars.SupermatterYellTimer));
            global = true;

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        if (sm.Delamming && sm.DelamAnnounced)
        {
            var mapId = Transform(uid).MapID;
            var mapFilter = Filter.BroadcastMap(mapId);

            var seconds = Math.Ceiling(sm.DelamEndTime.TotalSeconds - _timing.CurTime.TotalSeconds);

            if (seconds <= 0)
                return;

            var loc = seconds switch
            {
                > 5 => "supermatter-seconds-before-delam-countdown",
                <= 5 => "supermatter-seconds-before-delam-imminent",
                _ => string.Empty
            };

            sm.YellTimer = seconds switch
            {
                > 30 => TimeSpan.FromSeconds(10),
                > 5 => TimeSpan.FromSeconds(5),
                <= 5 => TimeSpan.FromSeconds(1),
                _ => TimeSpan.FromSeconds(_config.GetCVar(ADTCCVars.SupermatterYellTimer))
            };

            if (seconds <= 5 && TryComp<SpeechComponent>(uid, out var speech))
                speech.SoundCooldownTime = 4.5f;

            message = Loc.GetString(loc, ("seconds", seconds));

            if (seconds == 5)
                _audio.PlayGlobal(sm.Count, mapFilter, true);

            global = true;
            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        if (sm.Damage < sm.DamageArchived && sm.Status >= SupermatterStatusType.Warning)
        {
            message = Loc.GetString("supermatter-healing", ("integrity", integrity));

            if (sm.Status >= SupermatterStatusType.Emergency)
                global = true;

            if (TryComp<SpeechComponent>(uid, out var speech))
                speech.SoundCooldownTime = 0.0f;

            SendSupermatterAnnouncement(uid, sm, message, global);
            return;
        }

        if (sm.Delamming)
            return;

        if (sm.Damage <= sm.DamageArchived)
            return;

        bool TypeCascade = _config.GetCVar(ADTCCVars.SupermatterDoCascadeDelam) && sm.ResonantFrequency >= 1;

        if (sm.Damage >= sm.DamageWarningThreshold)
        {   
            message = TypeCascade
                ? Loc.GetString("supermatter-warning-cascade", ("integrity", integrity))
                : Loc.GetString("supermatter-warning", ("integrity", integrity));

            if (sm.Damage >= sm.DamageEmergencyThreshold)
            {
                message = TypeCascade
                    ? Loc.GetString("supermatter-emergency-cascade", ("integrity", integrity))
                    : Loc.GetString("supermatter-emergency", ("integrity", integrity));

                global = true;
            }
        
            SendSupermatterAnnouncement(uid, sm, message, global);
            global = false;

            if (sm.Power >= _config.GetCVar(ADTCCVars.SupermatterPowerPenaltyThreshold))
            {
                message = Loc.GetString("supermatter-threshold-power");
                SendSupermatterAnnouncement(uid, sm, message, global);

                if (sm.PowerlossInhibitor < 0.5)
                {
                    message = Loc.GetString("supermatter-threshold-powerloss");
                    SendSupermatterAnnouncement(uid, sm, message, global);
                }
            }

            if (sm.GasStorage != null && sm.GasStorage.TotalMoles >= _config.GetCVar(ADTCCVars.SupermatterMolePenaltyThreshold))
            {
                message = Loc.GetString("supermatter-threshold-mole");
                SendSupermatterAnnouncement(uid, sm, message, global);
            }
        }
    }

    /// <summary>
    /// Sends the given message to local chat and a radio channel
    /// </summary>
    /// <param name="global">If true, sends the message to the common radio</param>
    public void SendSupermatterAnnouncement(EntityUid uid, SupermatterComponent sm, string message, bool global = false)
    {
        if (sm.SuppressAnnouncements)
            return;

        if (message == String.Empty)
            return;

        var channel = sm.Channel;

        if (global)
            channel = sm.ChannelGlobal;

        sm.YellLast = _timing.CurTime;
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false, checkRadioPrefix: true);
        _radio.SendRadioMessage(uid, message, channel, uid);
    }

    /// <summary>
    /// Returns the integrity rounded to hundreds, e.g. 100.00%
    /// </summary>
    public static float GetIntegrity(SupermatterComponent sm)
    {
        var integrity = sm.Damage / sm.DamageDelaminationPoint;
        integrity = (float)Math.Round(100 - integrity * 100, 2);
        integrity = integrity < 0 ? 0 : integrity;
        return integrity;
    }
}