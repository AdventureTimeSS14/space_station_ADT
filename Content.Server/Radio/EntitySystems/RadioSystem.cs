using Content.Server.Access.Components;
using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Research.Components;
using Content.Server.VoiceMask;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Speech;
using Content.Shared.StatusIcon;
using Content.Shared.VendingMachines;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;
using Content.Server.ADT.Language;  // ADT Languages
using Content.Shared.ADT.Language;  // ADT Languages
using Content.Shared.ADT.Loudspeaker.Events;
using Content.Shared.Silicons.StationAi;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly LanguageSystem _language = default!;  // ADT Languages
    [Dependency] private readonly AccessReaderSystem _accessReader = default!; // ADT-Tweak
    [Dependency] private readonly InventorySystem _inventorySystem = default!; // ADT-Tweak

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    private EntityQuery<TelecomExemptComponent> _exemptQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);

        _exemptQuery = GetEntityQuery<TelecomExemptComponent>();
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent args)
    {
        if (TryComp(uid, out ActorComponent? actor))
        {
            // ADT Languages start
            if (_language.CanUnderstand(uid, args.Language))
                _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.Channel);
            else
                _netMan.ServerSendMessage(args.UnknownLanguageChatMsg, actor.PlayerSession.Channel);
            // ADT Languages end
        }
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(EntityUid messageSource, string message, ProtoId<RadioChannelPrototype> channel, EntityUid radioSource, bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, _prototype.Index(channel), radioSource, escapeMarkup: escapeMarkup);
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    public void SendRadioMessage(EntityUid messageSource, string message, RadioChannelPrototype channel, EntityUid radioSource, bool escapeMarkup = true, LanguagePrototype? languageOverride = null)
    {
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var language = languageOverride ?? _language.GetCurrentLanguage(messageSource);
        if (language.LanguageType is not Generic gen)
            return;

        // var name = TryComp(messageSource, out VoiceMaskComponent? mask) && mask.Enabled
        //     ? mask.VoiceName
        //     : MetaData(messageSource).EntityName;                                        // ADT Закоментировал чтобы не было ошибок
        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.Resolve(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        // ADT-tweak-start
        int? loudSpeakFont = null;

        var getLoudspeakerEv = new GetLoudspeakerEvent();
        RaiseLocalEvent(messageSource, ref getLoudspeakerEv);

        if (getLoudspeakerEv.Loudspeakers != null)
        {
            foreach (var loudspeaker in getLoudspeakerEv.Loudspeakers)
            {
                var loudSpeakerEv = new GetLoudspeakerDataEvent();
                RaiseLocalEvent(loudspeaker, ref loudSpeakerEv);

                if (loudSpeakerEv.IsActive && loudSpeakerEv.AffectRadio)
                {
                    loudSpeakFont = loudSpeakerEv.FontSize;
                    break;
                }
            }
        }
// ADT-Tweak-end

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        // ADT Languages start
        var languageEncodedContent = _language.ObfuscateMessage(messageSource, content, gen.Replacement, gen.ObfuscateSyllables, gen.ReplaceEntireMessage);

        if (gen.Color != null)
        {
            content = $"[color={gen.Color.Value.ToHex()}]{FormattedMessage.EscapeText(content)}[/color]";
            languageEncodedContent = $"[color={gen.Color.Value.ToHex()}]{FormattedMessage.EscapeText(languageEncodedContent)}[/color]";
        }

        List<string> verbStrings = speech.SpeechVerbStrings;
        bool verbsReplaced = false;
        foreach (var str in ILanguageType.SpeechSuffixes)
        {
            if (message.EndsWith(Loc.GetString(str)) && gen.SuffixSpeechVerbs.TryGetValue(str, out var strings) && strings.Count > 0)
            {
                verbStrings = strings;
                verbsReplaced = true;
            }
        }

        if (!verbsReplaced && gen.SuffixSpeechVerbs.TryGetValue("Default", out var defaultStrings) && defaultStrings.Count > 0)
            verbStrings = defaultStrings;
        // ADT Languages end

        // ADT-Tweak start
        var (iconId, jobName) = GetJobIcon(messageSource);
        var (langIconPath, langIconState) = GetLanguageIcon(language);

        var nameWithIcons = BuildNameWithIcons(name, iconId, jobName, langIconPath, langIconState, language);
        // ADT-Tweak end

        var wrappedMessage = Loc.GetString("chat-radio-message-wrap",   // ADT Languages tweak - remove bold
            ("color", channel.Color),
            ("fontType", gen.Font ?? speech.FontId),    // ADT Languages tweak speech.FontId -> gen.Font ?? speech.FontId
            ("fontSize", loudSpeakFont ?? speech.FontSize), // ADT-Tweak: loudspeaker font size override
            ("verb", Loc.GetString(_random.Pick(verbStrings))), // ADT Languages speech.SpeechVerbStrings -> verbStrings
            ("defaultFont", speech.FontId), // ADT Languages
            ("defaultSize", speech.FontSize),   // ADT Languages
            ("channel", $"\\[{channel.LocalizedName}\\]"),
            ("name", nameWithIcons), // ADT-Tweak
            ("message", content));

        // ADT Languages start
        var wrappedEncodedMessage = Loc.GetString("chat-radio-message-wrap",
            ("color", channel.Color),
            ("fontType", gen.Font ?? speech.FontId),
            ("fontSize", gen.FontSize ?? speech.FontSize),
            ("verb", Loc.GetString(_random.Pick(verbStrings))),
            ("defaultFont", speech.FontId),
            ("defaultSize", speech.FontSize),
            ("channel", $"\\[{channel.LocalizedName}\\]"),
            ("name", nameWithIcons), // ADT-Tweak
            ("message", languageEncodedContent));
        // ADT Languages end

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = new ChatMessage(
            ChatChannel.Radio,
            message,
            wrappedMessage,
            NetEntity.Invalid,
            null);

        // ADT Languages start
        var encodedChat = new ChatMessage(
            ChatChannel.Radio,
            message,
            wrappedEncodedMessage,
            NetEntity.Invalid,
            null);
        // ADT Languages end

        var chatMsg = new MsgChatMessage { Message = chat };
        var encodedChatMsg = new MsgChatMessage { Message = encodedChat };  // ADT Languages

        var ev = new RadioReceiveEvent(message, messageSource, channel, radioSource, chatMsg, encodedChatMsg, language);    // ADT Languages

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);
        }

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.RecordServerMessage(chat);
        _messages.Remove(message);
    }

    // ADT-Tweak start
    /// <summary>
    /// Gets the job icon ID and localized job name for an entity.
    /// Voice mask has highest priority, then ID cards/PDAs, then borg/AI.
    /// </summary>
    /// <param name="messageSource">The entity to get job icon info for</param>
    /// <returns>Tuple of (jobIconId, localizedJobName)</returns>
    private (string iconId, string jobName) GetJobIcon(EntityUid messageSource)
    {
        var iconId = "JobIconNoId";
        string? jobName = null;

        if (!Exists(messageSource) || Deleted(messageSource))
            return (iconId, jobName ?? string.Empty);

        if (TryGetActiveVoiceMaskJobIcon(messageSource, out var maskIconId, out var maskJobName))
        {
            return (maskIconId, maskJobName);
        }

        if (_accessReader.FindAccessItemsInventory(messageSource, out var items))
        {
            foreach (var item in items)
            {
                if (!Exists(item) || Deleted(item))
                    continue;

                if (TryComp<IdCardComponent>(item, out var idCard))
                {
                    iconId = idCard.JobIcon;
                    jobName = idCard.LocalizedJobTitle;
                    break;
                }

                if (TryComp<PdaComponent>(item, out var pda) && pda.ContainedId.HasValue)
                {
                    var containedId = pda.ContainedId.Value;
                    if (Exists(containedId) && !Deleted(containedId) && TryComp<IdCardComponent>(containedId, out var pdaIdCard))
                    {
                        iconId = pdaIdCard.JobIcon;
                        jobName = pdaIdCard.LocalizedJobTitle;
                        break;
                    }
                }
            }
        }

        if (iconId == "JobIconNoId")
        {
            if (HasComp<BorgChassisComponent>(messageSource))
            {
                iconId = "JobIconBorg";
                jobName = Loc.GetString("job-name-borg");
            }
            else if (HasComp<StationAiHeldComponent>(messageSource))
            {
                iconId = "JobIconStationAi";
                jobName = Loc.GetString("job-name-station-ai");
            }
            else if (HasComp<ResearchConsoleComponent>(messageSource) || HasComp<VendingMachineComponent>(messageSource))
            {
                iconId = "JobIconMachine";
                jobName = Loc.GetString("job-name-machine");
            }
        }

        return (iconId, jobName ?? string.Empty);
    }

    private bool TryGetActiveVoiceMaskJobIcon(EntityUid entity, out string iconId, out string jobName)
    {
        iconId = string.Empty;
        jobName = string.Empty;

        if (!TryComp<InventoryComponent>(entity, out var _))
            return false;

        var enumerator = _inventorySystem.GetSlotEnumerator(entity);
        while (enumerator.NextItem(out var item, out var slot))
        {
            if (slot == null || Deleted(item))
                continue;

            if (TryComp<VoiceMaskComponent>(item, out var voiceMask) && voiceMask.VoiceMaskJobIcon.HasValue)
            {
                iconId = voiceMask.VoiceMaskJobIcon.Value;
                jobName = _prototype.TryIndex<JobIconPrototype>(iconId, out var proto)
                    ? proto.LocalizedJobName
                    : Loc.GetString("voice-mask-job-icon-none");
                return true;
            }
        }

        return false;
    }

    private string WrapNameWithJobIcon(EntityUid messageSource, string name)
    {
        var (iconId, jobName) = GetJobIcon(messageSource);

        jobName = FormattedMessage.EscapeText(jobName);

        return Loc.GetString("chat-radio-name-with-job-icon",
            ("iconId", iconId),
            ("jobName", jobName),
            ("name", name));
    }

    private (string? path, string? state) GetLanguageIcon(LanguagePrototype language)
    {
        if (language.Icon is SpriteSpecifier.Rsi rsi)
        {
            return (rsi.RsiPath.ToString(), rsi.RsiState);
        }
        return (null, null);
    }

    private string BuildNameWithIcons(string name, string iconId, string jobName, string? langIconPath, string? langIconState, LanguagePrototype language)
    {
        jobName = FormattedMessage.EscapeText(jobName);
        var languageName = FormattedMessage.EscapeText(language.LocalizedName);

        if (langIconPath != null && langIconState != null)
        {
            var jobIconTag = $"[icon src=\"{iconId}\" tooltip=\"{jobName}\"]";
            var langIconTag = $"[icon src=\"{langIconPath}:{langIconState}\" tooltip=\"{languageName}\"]";
            return $"{langIconTag} {jobIconTag} {name}";
        }

        return Loc.GetString("chat-radio-name-with-job-icon",
            ("iconId", iconId),
            ("jobName", jobName),
            ("name", name));
    }
    // ADT-Tweak end

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}
