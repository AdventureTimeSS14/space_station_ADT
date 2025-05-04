using Content.Server.ADT.Hallucinations;
using Content.Shared.ADT.Supermatter.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Components;
using Content.Shared.Radiation.Components;
using Content.Shared.Light.Components;

namespace Content.Server.ADT.Supermatter.Systems;
public sealed partial class SupermatterSystem
{
    /// <summary>
    /// Scales the energy and radius of the supermatter's light based on its power
    /// </summary>
    public void HandleLight(EntityUid uid, SupermatterComponent sm)
    {
        if (!TryComp<PointLightComponent>(uid, out var light))
            return;

        var scalar = Math.Clamp(sm.Power / 2500f + 1f, 1f, 2f);
        var integrity = GetIntegrity(sm);
        var hsvNormal = Color.ToHsv(sm.LightColorNormal);
        var hsvDelam = Color.ToHsv(sm.LightColorDelam);
        var hsvFinal = Vector4.Lerp(hsvDelam, hsvNormal, integrity / 100f);

        _light.SetEnergy(uid, 2f * scalar, light);
        _light.SetRadius(uid, 10f * scalar, light);
        _light.SetColor(uid, Color.FromHsv(hsvFinal), light);
    }

    /// <summary>
    /// Applies hallucinations and psychologist coefficient
    /// </summary>
    public void HandleVision(EntityUid uid, SupermatterComponent sm)
    {
        var psyDiff = -0.007f;
        var lookup = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(uid).Coordinates, 20f);

        foreach (var mob in lookup)
        {
            if (!_examine.InRangeUnOccluded(uid, mob, 20f) ||
                mob.Comp.CurrentState == MobState.Dead)
                continue;

            if (HasComp<SupermatterSootherComponent>(mob))
                psyDiff = 0.007f;

            if (HasComp<SupermatterHallucinationImmuneComponent>(mob) || 
                HasComp<SiliconLawBoundComponent>(mob) ||
                HasComp<PermanentBlindnessComponent>(mob) || 
                HasComp<TemporaryBlindnessComponent>(mob))
                continue;

            var paracusiaSounds = new SoundCollectionSpecifier("Paracusia");
            var paracusiaMinTime = 0.1f;
            var paracusiaMaxTime = 300f;
            var paracusiaDistance = 7f;

            if (!EnsureComp<ParacusiaComponent>(mob, out var paracusia))
            {
                _paracusia.SetSounds(mob, paracusiaSounds, paracusia);
                _paracusia.SetTime(mob, paracusiaMinTime, paracusiaMaxTime, paracusia);
                _paracusia.SetDistance(mob, paracusiaDistance, paracusia);
            }

            var hallucinationKey = "ADTHallucination";
            var hallucinationProto = "Supermatter";
            var hallucinationDuration = TimeSpan.FromSeconds(10);
            var refresh = true;

            _hallucinations.StartHallucinations(mob, hallucinationKey, hallucinationDuration, refresh, hallucinationProto);
        }

        sm.PsyCoefficient = Math.Clamp(sm.PsyCoefficient + psyDiff, 0f, 1f);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, SupermatterVisuals.Psy, sm.PsyCoefficient, appearance);
    }

    /// <summary>
    /// Swaps out ambience sounds when the SM is delamming or not
    /// </summary>
    public void HandleSoundLoop(EntityUid uid, SupermatterComponent sm)
    {
        if (!TryComp<AmbientSoundComponent>(uid, out var ambient))
            return;

        var volume = (float)Math.Round(Math.Clamp(sm.Power / 50 - 5, -5, 5));
        _ambient.SetVolume(uid, volume);

        if (sm.Status >= SupermatterStatusType.Danger && sm.CurrentSoundLoop != sm.DelamLoopSound)
            sm.CurrentSoundLoop = sm.DelamLoopSound;
        else if (sm.Status < SupermatterStatusType.Danger && sm.CurrentSoundLoop != sm.CalmLoopSound)
            sm.CurrentSoundLoop = sm.CalmLoopSound;

        if (ambient.Sound != sm.CurrentSoundLoop)
            _ambient.SetSound(uid, sm.CurrentSoundLoop!, ambient);
    }

    /// <summary>
    /// Plays normal/delam sounds at a rate determined by power and damage
    /// </summary>
    public void HandleAccent(EntityUid uid, SupermatterComponent sm)
    {
        if (sm.AccentLastTime >= _timing.CurTime || !_random.Prob(0.05f))
            return;

        var aggression = Math.Min((sm.Damage / 800) * (sm.Power / 2500), 1) * 100;
        var nextSound = Math.Max(Math.Round((100 - aggression) * 5), sm.AccentMinCooldown);
        var sound = sm.CalmAccent;

        if (sm.AccentLastTime + TimeSpan.FromSeconds(nextSound) > _timing.CurTime)
            return;

        if (sm.Status >= SupermatterStatusType.Danger)
            sound = sm.DelamAccent;

        sm.AccentLastTime = _timing.CurTime;
        _audio.PlayPvs(sound, Transform(uid).Coordinates);
    }
}