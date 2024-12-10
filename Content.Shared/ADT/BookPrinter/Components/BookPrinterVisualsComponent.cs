using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
