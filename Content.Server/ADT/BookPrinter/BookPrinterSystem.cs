using System.Linq;
using JetBrains.Annotations;
using Content.Server.ADT.BookPrinter.Components;
using Content.Server.Database;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Shared.ADT.BookPrinter;
using Content.Shared.ADT.BookPrinter.Components;
using Content.Shared.Examine;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Content.Shared.Power;
using Content.Shared.Tag;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Labels.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Construction.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.ADT.BookPrinter
{
    [UsedImplicitly]
    public sealed partial class BookPrinterSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly TagSystem _tag = default!;

        public readonly List<SharedBookPrinterEntry> BookPrinterEntries = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BookPrinterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<BookPrinterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<BookPrinterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<BookPrinterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<BookPrinterComponent, BookPrinterClearContainerMessage>(OnClearContainerMessage);
            SubscribeLocalEvent<BookPrinterComponent, BookPrinterUploadMessage>(OnUploadMessage);
            SubscribeLocalEvent<BookPrinterComponent, BookPrinterPrintBookMessage>(OnPrintBookMessage);
            SubscribeLocalEvent<BookPrinterComponent, BookPrinterCopyPasteMessage>(OnCopyPasteMessage);
            SubscribeLocalEvent<BookPrinterComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<BookPrinterComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<BookPrinterCartridgeComponent, ExaminedEvent>(OnExamined);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<BookPrinterComponent, ApcPowerReceiverComponent>();
            while (query.MoveNext(out var uid, out var printer, out var receiver))
            {
                if (!Transform(uid).Anchored || !receiver.Powered)
                {
                    FlushTask((uid, printer));
                    SetLockOnAllSlots((uid, printer), !receiver.Powered && Transform(uid).Anchored);
                    continue;
                }

                if (printer.WorkType is not null && printer.WorkTimeRemaining > 0.0f)
                {
                    printer.WorkTimeRemaining -= frameTime * printer.TimeMultiplier;
                    if (printer.WorkTimeRemaining <= 0.0f)
                        ProcessTask((uid, printer));
                }
            }
        }

        private void UpdateVisuals(Entity<BookPrinterComponent> ent)
        {
            var cartridge = _itemSlotsSystem.GetItemOrNull(ent, "cartridgeSlot");

            var workInProgress = ent.Comp.WorkType is not null && ent.Comp.WorkTimeRemaining > 0.0f;

            _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Working, workInProgress);

            if (EntityManager.TryGetComponent<BookPrinterVisualsComponent>(ent, out BookPrinterVisualsComponent? visualsComp))
            {
                visualsComp.DoWorkAnimation = workInProgress;
            }

            if (cartridge is not null && EntityManager.TryGetComponent<BookPrinterCartridgeComponent>(cartridge, out BookPrinterCartridgeComponent? cartridgeComp))
            {
                _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Slotted, true);
                _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Full, cartridgeComp.CurrentCharge == cartridgeComp.FullCharge);
                _appearanceSystem.SetData(ent, BookPrinterVisualLayers.High, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 1.43f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge);
                _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Medium, cartridgeComp.CurrentCharge >= cartridgeComp.FullCharge / 2.5f && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 1.43f);
                _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Low, cartridgeComp.CurrentCharge > 0 && cartridgeComp.CurrentCharge < cartridgeComp.FullCharge / 2.5f);
                _appearanceSystem.SetData(ent, BookPrinterVisualLayers.None, cartridgeComp.CurrentCharge < 1);

                return;
            }

            _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Slotted, true);
            _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Full, false);
            _appearanceSystem.SetData(ent, BookPrinterVisualLayers.High, false);
            _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Medium, false);
            _appearanceSystem.SetData(ent, BookPrinterVisualLayers.Low, false);
            _appearanceSystem.SetData(ent, BookPrinterVisualLayers.None, false);
        }

        private void SubscribeUpdateUiState<T>(Entity<BookPrinterComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
            RefreshBookContent();
        }

        private bool IsRoutineAllowed(Entity<BookPrinterComponent> bookPrinter)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "bookSlot");
            var cartridgeContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "cartridgeSlot");

            return bookContainer is not null &&
                cartridgeContainer is not null &&
                TryComp<BookPrinterCartridgeComponent>(cartridgeContainer, out var cartridgeComp) &&
                cartridgeComp.CurrentCharge > bookPrinter.Comp.CartridgeUsage &&
                cartridgeComp.FullCharge > bookPrinter.Comp.CartridgeUsage;
        }

        private void DecreaseCartridgeCharge(Entity<BookPrinterComponent> bookPrinter)
        {
            var cartridgeContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "cartridgeSlot");
            if (!TryComp<BookPrinterCartridgeComponent>(cartridgeContainer, out var cartridgeComp)
                || cartridgeComp.CurrentCharge < bookPrinter.Comp.CartridgeUsage)
                return;

            cartridgeComp.CurrentCharge -= bookPrinter.Comp.CartridgeUsage;
        }

        private bool TryLowerCartridgeCharge(Entity<BookPrinterComponent> bookPrinter)
        {
            if (!IsRoutineAllowed(bookPrinter))
            {
                _popupSystem.PopupEntity(Loc.GetString("book-printer-cartridge-component-empty"), bookPrinter);
                return false;
            }

            DecreaseCartridgeCharge(bookPrinter);
            return true;
        }

        private void OnExamined(EntityUid uid, BookPrinterCartridgeComponent component, ExaminedEvent args)
        {
            args.PushText(Loc.GetString("book-printer-cartridge-component-examine", ("charge", (int)(component.CurrentCharge / component.FullCharge * 100))));
        }

        private void OnPowerChanged(EntityUid uid, BookPrinterComponent component, ref PowerChangedEvent args)
        {
            FlushTask((uid, component));

            SetLockOnAllSlots((uid, component), !args.Powered);

            UpdateVisuals((uid, component));
        }

        private void OnUnanchorAttempt(EntityUid uid, BookPrinterComponent component, UnanchorAttemptEvent args)
        {
            if (!Transform(uid).Anchored)
                FlushTask((uid, component));
        }

        private void UpdateUiState(Entity<BookPrinterComponent> bookPrinter)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "bookSlot");
            var cartridgeContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "cartridgeSlot");
            var bookName = bookContainer is not null ? Name(bookContainer.Value) : null;
            var bookDescription = bookContainer is not null ? Description(bookContainer.Value) : null;
            float? cartridgeCharge = cartridgeContainer is not null ?
                                    EntityManager.TryGetComponent<BookPrinterCartridgeComponent>(cartridgeContainer, out var cartridgeComp) ?
                                    cartridgeComp.CurrentCharge / cartridgeComp.FullCharge :
                                    null :
                                    null;

            float? workProgress = bookPrinter.Comp.WorkTimeRemaining > 0.0f && bookPrinter.Comp.WorkType is not null ?
                                    bookPrinter.Comp.WorkTimeRemaining / bookPrinter.Comp.WorkTime : null;

            var state = new BookPrinterBoundUserInterfaceState(bookName,
                            bookDescription,
                            GetNetEntity(bookContainer),
                            BookPrinterEntries,
                            IsRoutineAllowed(bookPrinter),
                            cartridgeCharge,
                            workProgress,
                            bookPrinter.Comp.PrintBookEntry is not null);
            _userInterfaceSystem.SetUiState(bookPrinter.Owner, BookPrinterUiKey.Key, state);
            UpdateVisuals(bookPrinter);
        }

        private void SetLockOnAllSlots(Entity<BookPrinterComponent> bookPrinter, bool lockValue)
        {
            _itemSlotsSystem.SetLock(bookPrinter, "cartridgeSlot", lockValue);
            _itemSlotsSystem.SetLock(bookPrinter, "bookSlot", lockValue);
        }

        private void OnClearContainerMessage(Entity<BookPrinterComponent> bookPrinter, ref BookPrinterClearContainerMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "bookSlot");
            if (bookContainer is not { Valid: true })
                return;

            if (message.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            if (IsAuthorized(bookPrinter, entity, bookPrinter) && TryLowerCartridgeCharge(bookPrinter))
                SetupTask(bookPrinter, "Clearing");

            UpdateUiState(bookPrinter);
        }

        private void OnUploadMessage(Entity<BookPrinterComponent> bookPrinter, ref BookPrinterUploadMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "bookSlot");
            if (bookContainer is not { Valid: true })
                return;

            if (message.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            if (IsAuthorized(bookPrinter, entity, bookPrinter) && TryLowerCartridgeCharge(bookPrinter))
            {
                var content = GetContent(bookContainer.Value);
                if (content is not null)
                    UploadBookContent(content);
                SetupTask(bookPrinter, "Uploading");
            }

            UpdateUiState(bookPrinter);
        }

        private void OnPrintBookMessage(Entity<BookPrinterComponent> bookPrinter, ref BookPrinterPrintBookMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "bookSlot");
            if (bookContainer is not { Valid: true })
                return;
            if (BookPrinterEntries.Count() < 1)
                return;

            if (message.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            RefreshBookContent();

            if (IsAuthorized(bookPrinter, entity, bookPrinter) && TryLowerCartridgeCharge(bookPrinter))
            {
                bookPrinter.Comp.PrintBookEntry = message.BookEntry;
                SetupTask(bookPrinter, "Printing");
            }

            UpdateUiState(bookPrinter);
        }

        private void OnCopyPasteMessage(Entity<BookPrinterComponent> bookPrinter, ref BookPrinterCopyPasteMessage message)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "bookSlot");
            if (bookContainer is not { Valid: true })
                return;
            if (BookPrinterEntries.Count() < 1)
                return;

            if (message.Actor is not { Valid: true } entity || Deleted(entity))
                return;

            RefreshBookContent();

            if (IsAuthorized(bookPrinter, entity, bookPrinter))
            {
                if (bookPrinter.Comp.PrintBookEntry is null)
                {
                    bookPrinter.Comp.PrintBookEntry = GetContent(bookContainer.Value);
                }
                else if (TryLowerCartridgeCharge(bookPrinter))
                {
                    SetupTask(bookPrinter, "Printing");
                }

            }

            UpdateUiState(bookPrinter);
        }

        private void ProcessTask(Entity<BookPrinterComponent> bookPrinter)
        {
            var bookContainer = _itemSlotsSystem.GetItemOrNull(bookPrinter, "bookSlot");

            if (bookContainer is { Valid: true })
            {
                if (bookPrinter.Comp.WorkType == "Clearing" || bookPrinter.Comp.WorkType == "Printing")
                    ClearContent(bookContainer.Value);

                if (bookPrinter.Comp.WorkType == "Printing" && bookPrinter.Comp.PrintBookEntry is not null)
                    SetContent(bookContainer.Value, bookPrinter.Comp.PrintBookEntry, bookPrinter);

            }

            FlushTask(bookPrinter);
            UpdateUiState(bookPrinter);

            _audio.PlayPvs(bookPrinter.Comp.ClickSound, bookPrinter, AudioParams.Default.WithVolume(5f));

        }

        private void SetupTask(Entity<BookPrinterComponent> bookPrinter, string? taskName)
        {
            SetLockOnAllSlots(bookPrinter, true);
            bookPrinter.Comp.WorkTimeRemaining = bookPrinter.Comp.WorkTime;
            bookPrinter.Comp.WorkType = taskName;

            _audio.PlayPvs(bookPrinter.Comp.WorkSound, bookPrinter, AudioParams.Default.WithVolume(5f));
            _ambientSoundSystem.SetAmbience(bookPrinter, true);
            UpdateVisuals(bookPrinter);
        }

        private void FlushTask(Entity<BookPrinterComponent> bookPrinter)
        {
            RefreshBookContent();
            SetLockOnAllSlots(bookPrinter, false);
            bookPrinter.Comp.PrintBookEntry = null;
            bookPrinter.Comp.WorkType = null;
            bookPrinter.Comp.WorkTimeRemaining = 0.0f;

            _ambientSoundSystem.SetAmbience(bookPrinter, false);
            UpdateVisuals(bookPrinter);
            UpdateUiState(bookPrinter);
        }

        private void ClearContent(EntityUid? item)
        {
            if (item is null)
                return;

            if (EntityManager.TryGetComponent(item.Value, out PaperComponent? paperComp))
            {
                paperComp.StampedBy = new List<StampDisplayInfo>();
                paperComp.StampState = null;
                _paperSystem.SetContent((item.Value, paperComp), "", false);
                _paperSystem.UpdateStampState((item.Value, paperComp));
            }

            if (EntityManager.TryGetComponent<MetaDataComponent>(item.Value, out var metadata))
            {

                var newName = Loc.GetString("book-printer-unknown-name-blank");
                var newDesc = Loc.GetString("book-printer-unknown-description-blank");

                if (_tag.HasTag(item.Value, "Book"))
                {
                    newName = Loc.GetString("book-printer-book-name-blank");
                    newDesc = Loc.GetString("book-printer-book-description-blank");
                }

                _metaData.SetEntityName(item.Value, newName, metadata);
                _metaData.SetEntityDescription(item.Value, newDesc, metadata);
            }

            RemComp<LabelComponent>(item.Value);
        }

        private void SetContent(EntityUid? item, SharedBookPrinterEntry bookEntry, Entity<BookPrinterComponent> bookPrinter)
        {
            if (item is null)
                return;

            var paperComp = EnsureComp<PaperComponent>(item.Value);
            var metadata = EnsureComp<MetaDataComponent>(item.Value);

            _metaData.SetEntityName(item.Value, bookEntry.Name, metadata);
            _metaData.SetEntityDescription(item.Value, bookEntry.Description, metadata);
            _paperSystem.SetContent((item.Value, paperComp), bookEntry.Content, false);

            foreach (var stampEntry in bookEntry.StampedBy)
            {
                var stampInfo = new StampDisplayInfo
                {
                    StampedName = stampEntry.Name,
                    StampedColor = Color.FromHex(stampEntry.Color)
                };

                _paperSystem.TryStamp((item.Value, paperComp), stampInfo, "paper_stamp-void");
            }

            if (!HasComp<EmaggedComponent>(bookPrinter) && bookPrinter.Comp.StampedName is not null)
            {
                var stampedColor = bookPrinter.Comp.StampedColor is not null ?
                                    Color.FromHex(bookPrinter.Comp.StampedColor) :
                                    Color.FromHex("#000000");

                var stampInfo = new StampDisplayInfo
                {
                    StampedName = bookPrinter.Comp.StampedName,
                    StampedColor = stampedColor,
                };

                _paperSystem.TryStamp((item.Value, paperComp), stampInfo, "paper_stamp-void");
            }

            paperComp.StampState = bookEntry.StampState != "" ? bookEntry.StampState : null;
            _paperSystem.UpdateStampState((item.Value, paperComp));
        }

        public async void RefreshBookContent()
        {
            var db = IoCManager.Resolve<IServerDbManager>();
            var entries = await db.GetBookPrinterEntriesAsync();

            BookPrinterEntries.Clear();

            foreach (var entry in entries)
            {
                var convStampedBy = new List<SharedStampedData>();
                foreach (var stampEntry in entry.StampedBy)
                    convStampedBy.Add(new SharedStampedData(stampEntry.Id, stampEntry.Name, stampEntry.Color));

                BookPrinterEntries.Add(new SharedBookPrinterEntry(entry.Id, entry.Name, entry.Description, entry.Content, convStampedBy, entry.StampState));
            }
        }

        public SharedBookPrinterEntry? RetrieveBookContent(int id)
        {
            return BookPrinterEntries.Find(entry => entry.Id == id);
        }

        private SharedBookPrinterEntry? GetContent(EntityUid? item)
        {
            if (item is null)
                return null;

            var paperComp = EnsureComp<PaperComponent>(item.Value);
            var metadata = EnsureComp<MetaDataComponent>(item.Value);

            var sharedStamps = new List<SharedStampedData>();

            foreach (var entry in paperComp.StampedBy)
            {
                sharedStamps.Add(new SharedStampedData(-1, entry.StampedName, entry.StampedColor.ToHex()));
            }

            return new SharedBookPrinterEntry(-1,
                Name(item.Value) ?? "",
                Description(item.Value) ?? "",
                paperComp.Content ?? "",
                sharedStamps,
                paperComp.StampState ?? "paper_stamp-void");
        }

        public async void UploadBookContent(SharedBookPrinterEntry sharedBookEntry)
        {
            var db = IoCManager.Resolve<IServerDbManager>();

            BookPrinterEntry bookEntry = new BookPrinterEntry();

            List<StampedData> stampData = new List<StampedData>();

            foreach (var entry in sharedBookEntry.StampedBy)
            {
                var entryData = new StampedData();
                entryData.Name = entry.Name;
                entryData.Color = entry.Color;
                stampData.Add(entryData);
            }

            bookEntry.Name = sharedBookEntry.Name;
            bookEntry.Description = sharedBookEntry.Description;
            bookEntry.Content = sharedBookEntry.Content;
            bookEntry.StampState = sharedBookEntry.StampState;
            bookEntry.StampedBy = stampData;

            await db.UploadBookPrinterEntryAsync(bookEntry);

            RefreshBookContent();
        }

        public bool IsAuthorized(EntityUid uid, EntityUid user, BookPrinterComponent? bookPrinter = null)
        {
            if (!Resolve(uid, ref bookPrinter))
                return false;

            if (!TryComp<AccessReaderComponent>(uid, out var accessReader))
                return true;

            if (_accessReader.IsAllowed(user, uid, accessReader) || HasComp<EmaggedComponent>(uid))
                return true;

            _popupSystem.PopupEntity(Loc.GetString("book-printer-component-access-denied"), uid);
            return false;
        }
    }
}


