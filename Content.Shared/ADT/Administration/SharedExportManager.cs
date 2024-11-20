using Robust.Shared.Network;


namespace Content.Shared.ADT.Export
{
    public abstract class SharedExportManager
    {
        [Dependency] protected readonly INetManager NetManager = default!;

        public void Initialize()
        {
            NetManager.RegisterNetMessage<ExportYmlMessage>(Load);
        }

        public virtual void Load(ExportYmlMessage msg)
        {
        }
    }
}
