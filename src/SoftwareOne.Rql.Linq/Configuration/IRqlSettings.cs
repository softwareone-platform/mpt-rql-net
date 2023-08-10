#pragma warning disable IDE0130

namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSettings
    {
        MemberFlag DefaultFlags { get; set; }
        IRqlSelectSettings Select { get; }
    }
}
