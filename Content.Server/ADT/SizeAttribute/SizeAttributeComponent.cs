using Content.Shared.Cloning;

namespace Content.Server.ADT.SizeAttribute
{
    [RegisterComponent]
    public sealed partial class SizeAttributeComponent : Component, ITransferredByCloning
    {
        [DataField("short")]
        public bool Short = false;

        [DataField("tall")]
        public bool Tall = false;
    }
}