#pragma warning disable IDE0130

namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSettings
    {
        MemberFlag DefaultFlags { get; set; }
        IRqlSelectSettings Select { get; }
    }

    public interface IRqlSelectSettings
    {
        SelectMode Mode { get; set; }
        int? MaxDepth { get; set; }
    }
}
