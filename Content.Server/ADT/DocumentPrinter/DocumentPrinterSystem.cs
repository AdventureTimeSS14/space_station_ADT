using Content.Server.GameTicking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.DocumentPrinter;

public sealed class DocumentPrinterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DocumentPrinterComponent, PrintingDocumentEvent>(OnPrinting);
    }

    public void OnPrinting(EntityUid uid, DocumentPrinterComponent component, PrintingDocumentEvent args)
    {
    }
}
