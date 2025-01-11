// Оригинал данного файла был сделан @temporaldarkness (discord). Код был взят с https://github.com/ss14-ganimed/ENT14-Master.

using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BookPrinter.Components
{

    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class BookPrinterVisualsComponent : Component
    {
        [DataField("doWorkAnimation"), AutoNetworkedField]
        public bool DoWorkAnimation = false;
    }

    [Serializable, NetSerializable]
    public enum BookPrinterVisualLayers : byte
    {
        Base,
        Working,
        Slotted,
        Full,
        High,
        Medium,
        Low,
        None
    }
}
