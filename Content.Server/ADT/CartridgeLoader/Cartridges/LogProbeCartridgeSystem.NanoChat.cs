using Content.Shared.Audio;
using Content.Shared.CartridgeLoader;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.NanoChat;
using Robust.Shared.Random;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed partial class LogProbeCartridgeSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    private void InitializeNanoChat()
    {
        SubscribeLocalEvent<NanoChatRecipientUpdatedEvent>(OnRecipientUpdated);
        SubscribeLocalEvent<NanoChatMessageReceivedEvent>(OnMessageReceived);
    }

    private void OnRecipientUpdated(ref NanoChatRecipientUpdatedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData == null || GetEntity(probe.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                new Dictionary<uint, NanoChatRecipient>(card.Recipients),
                probe.ScannedNanoChatData.Value.Messages,
                new Dictionary<uint, NanoChatGroup>(card.Groups),
                probe.ScannedNanoChatData.Value.GroupMessages,
                card.Number,
                GetNetEntity(args.CardUid));

            if (cartridge.LoaderUid != null)
                UpdateUiState((uid, probe), cartridge.LoaderUid.Value);
        }
    }

    private void OnMessageReceived(ref NanoChatMessageReceivedEvent args)
    {
        var query = EntityQueryEnumerator<LogProbeCartridgeComponent, CartridgeComponent>();
        while (query.MoveNext(out var uid, out var probe, out var cartridge))
        {
            if (probe.ScannedNanoChatData == null || GetEntity(probe.ScannedNanoChatData.Value.Card) != args.CardUid)
                continue;

            if (!TryComp<NanoChatCardComponent>(args.CardUid, out var card))
                continue;

            probe.ScannedNanoChatData = new NanoChatData(
                probe.ScannedNanoChatData.Value.Recipients,
                new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
                probe.ScannedNanoChatData.Value.Groups,
                new Dictionary<uint, List<NanoChatMessage>>(card.GroupMessages),
                card.Number,
                GetNetEntity(args.CardUid));

            if (cartridge.LoaderUid != null)
                UpdateUiState((uid, probe), cartridge.LoaderUid.Value);
        }
    }

    private void ScanNanoChatCard(Entity<LogProbeCartridgeComponent> ent,
        CartridgeAfterInteractEvent args,
        EntityUid target,
        NanoChatCardComponent card)
    {
        _audio.PlayEntity(ent.Comp.SoundScan,
            args.InteractEvent.User,
            target,
            AudioHelpers.WithVariation(0.25f, _random));
        _popup.PopupCursor(Loc.GetString("log-probe-scan-nanochat", ("card", target)), args.InteractEvent.User);

        ent.Comp.PulledAccessLogs.Clear();

        ent.Comp.ScannedNanoChatData = new NanoChatData(
            new Dictionary<uint, NanoChatRecipient>(card.Recipients),
            new Dictionary<uint, List<NanoChatMessage>>(card.Messages),
            new Dictionary<uint, NanoChatGroup>(card.Groups),
            new Dictionary<uint, List<NanoChatMessage>>(card.GroupMessages),
            card.Number,
            GetNetEntity(target)
        );

        UpdateUiState(ent, args.Loader);
    }
}
