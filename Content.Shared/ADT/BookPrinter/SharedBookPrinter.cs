// Оригинал данного файла был сделан @temporaldarkness (discord). Код был взят с https://github.com/ss14-ganimed/ENT14-Master.

using Robust.Shared.Serialization;

namespace Content.Shared.ADT.BookPrinter
{

    [Serializable, NetSerializable]
    public sealed class BookPrinterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string? BookName;
        public readonly string? BookDescription;
        public readonly NetEntity? BookEntity;
        public readonly List<SharedBookPrinterEntry>? BookEntries;
        public readonly bool RoutineAllowed;
        public readonly float? CartridgeCharge;
        public readonly float? WorkProgress;
        public readonly bool CopyPaste;
        public readonly bool IsCooldownEnabled;
        public readonly TimeSpan CooldownRemaining;
        public readonly TimeSpan CooldownDuration;
        public readonly bool IsUploadAvailable;

        public BookPrinterBoundUserInterfaceState(string? bookName, string? bookDescription, NetEntity? bookEntity,
        List<SharedBookPrinterEntry>? bookEntries, bool routineAllowed = false, float? cartridgeCharge = null,
        float? workProgress = null, bool copyPaste = false, bool isCooldownEnabled = false, TimeSpan cooldownRemaining = default,
        TimeSpan cooldownDuration = default, bool isUploadAvailable = true)
        {
            BookName = bookName;
            BookDescription = bookDescription;
            BookEntity = bookEntity;
            BookEntries = bookEntries;
            RoutineAllowed = routineAllowed;
            CartridgeCharge = cartridgeCharge;
            WorkProgress = workProgress;
            CopyPaste = copyPaste;
            IsCooldownEnabled = isCooldownEnabled;
            CooldownRemaining = cooldownRemaining;
            CooldownDuration = cooldownDuration;
            IsUploadAvailable = isUploadAvailable;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BookPrinterPrintBookMessage : BoundUserInterfaceMessage
    {
        public readonly SharedBookPrinterEntry BookEntry;

        public BookPrinterPrintBookMessage(SharedBookPrinterEntry bookEntry)
        {
            BookEntry = bookEntry;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BookPrinterClearContainerMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class BookPrinterUploadMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class BookPrinterCopyPasteMessage : BoundUserInterfaceMessage
    {

    }

    [Serializable, NetSerializable]
    public sealed class SharedBookPrinterEntry
    {
        public int Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Content { get; set; } = default!;
        public List<SharedStampedData> StampedBy { get; set; } = default!;
        public string StampState { get; set; } = default!;

        public SharedBookPrinterEntry(int id, string name, string description, string content, List<SharedStampedData> stampedBy, string stampState)
        {
            Id = id;
            Name = name;
            Description = description;
            Content = content;
            StampedBy = stampedBy;
            StampState = stampState;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SharedStampedData
    {
        public int Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Color { get; set; } = default!;

        public SharedStampedData(int id, string name, string color)
        {
            Id = id;
            Name = name;
            Color = color;
        }
    }

    [Serializable, NetSerializable]
    public enum BookPrinterUiKey
    {
        Key
    }
}
