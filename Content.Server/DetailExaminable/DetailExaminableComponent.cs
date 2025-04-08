using Content.Shared.SD;
namespace Content.Server.DetailExaminable
{
    [RegisterComponent]
    public sealed partial class DetailExaminableComponent : Component
    {
        [DataField("content", required: true)] [ViewVariables(VVAccess.ReadWrite)]
        public string Content = "";
        // SD-ERPStatus-start
        [DataField("ERPStatus", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public EnumERPStatus ERPStatus = EnumERPStatus.NO;

        public string GetERPStatusName()
        {
            switch (ERPStatus)
            {
                case EnumERPStatus.HALF:
                    return Loc.GetString("humanoid-erp-status-half");
                case EnumERPStatus.FULL:
                    return Loc.GetString("humanoid-erp-status-full");
                default:
                    return Loc.GetString("humanoid-erp-status-no");
            }
        }
        // SD-ERPStatus-end
    }
}
