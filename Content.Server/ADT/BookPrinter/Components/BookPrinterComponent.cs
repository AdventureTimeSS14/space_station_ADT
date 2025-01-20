// Оригинал данного файла был сделан @temporaldarkness (discord). Код был взят с https://github.com/ss14-ganimed/ENT14-Master.

using Content.Server.ADT.BookPrinter;
using Content.Shared.ADT.BookPrinter;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.BookPrinter.Components
{
    [RegisterComponent]
    [Access(typeof(BookPrinterSystem))]
    public sealed partial class BookPrinterComponent : Component
    {

        [DataField]
        public SoundSpecifier WorkSound = new SoundPathSpecifier("/Audio/Machines/tray_eject.ogg");

		[DataField]
		public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/terminal_insert_disc.ogg");

		[DataField]
        public float WorkTimeRemaining = 0.0f;

		[DataField]
        public string? WorkType;

		[DataField, ViewVariables(VVAccess.ReadWrite)]
        public string? StampedName = "stamp-component-stamped-name-terminal";

		[DataField, ViewVariables(VVAccess.ReadWrite)]
        public string? StampedColor = "#999999";

		[DataField]
        public SharedBookPrinterEntry? PrintBookEntry;

		[DataField, ViewVariables(VVAccess.ReadWrite)]
		public float WorkTime = 8.0f;

		[DataField]
		public float TimeMultiplier = 1.0f;

		[DataField, ViewVariables(VVAccess.ReadWrite)]
		public float CartridgeUsage = 1.0f;
    }
}
