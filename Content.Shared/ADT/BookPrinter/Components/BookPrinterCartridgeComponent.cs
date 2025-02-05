// Оригинал данного файла был сделан @temporaldarkness (discord). Код был взят с https://github.com/ss14-ganimed/ENT14-Master.

using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BookPrinter.Components
{

    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class BookPrinterCartridgeComponent : Component
    {
        [DataField("fullCharge"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public float FullCharge = 20.0f;

        [DataField("currentCharge"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public float CurrentCharge = 20.0f;
    }
}
