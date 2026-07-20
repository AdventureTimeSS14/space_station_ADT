// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Blob;
using Content.Shared.ADT.Blob.Components;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Speech;
using Content.Server.ADT.Language;
using Content.Shared.ADT.Language;

namespace Content.Server.ADT.Blob;

public sealed class BlobMobSystem : SharedBlobMobSystem
{
    [Dependency] private readonly LanguageSystem _language = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobMobComponent, BlobMobGetPulseEvent>(OnPulsed);

        SubscribeLocalEvent<BlobSpeakComponent, ComponentStartup>(OnSpokeAdd);
        SubscribeLocalEvent<BlobSpeakComponent, ComponentShutdown>(OnSpokeRemove);
        SubscribeLocalEvent<BlobSpeakComponent, TransformSpeakerNameEvent>(OnSpokeName);
        SubscribeLocalEvent<BlobSpeakComponent, SpeakAttemptEvent>(OnSpokeCan, after: new []{ typeof(SpeechSystem) });
    }

    private void OnSpokeName(Entity<BlobSpeakComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (!ent.Comp.OverrideName)
        {
            return;
        }
        args.VoiceName = Loc.GetString(ent.Comp.Name);
    }

    private void OnSpokeCan(Entity<BlobSpeakComponent> ent, ref SpeakAttemptEvent args)
    {
        if (HasComp<BlobCarrierComponent>(ent))
        {
            return;
        }
        args.Uncancel();
    }

    private void OnSpokeRemove(Entity<BlobSpeakComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (TryComp<LanguageSpeakerComponent>(ent.Owner, out var langComp))
            langComp.Languages.Remove(ent.Comp.Language);
        // var radio = EnsureComp<ActiveRadioComponent>(ent);
        // radio.Channels.Remove(ent.Comp.Channel);
    }

    private void OnSpokeAdd(Entity<BlobSpeakComponent> ent, ref ComponentStartup args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        var component = EnsureComp<LanguageSpeakerComponent>(ent);
        _language.AddSpokenLanguage(ent.Owner, ent.Comp.Language, LanguageKnowledge.Speak, component);
        component.CurrentLanguage = ent.Comp.Language;

        // var radio = EnsureComp<ActiveRadioComponent>(ent);
        // radio.Channels.Add(ent.Comp.Channel);
    }

    private void OnPulsed(EntityUid uid, BlobMobComponent component, BlobMobGetPulseEvent args) =>
        _damageableSystem.TryChangeDamage(uid, component.HealthOfPulse);
}
