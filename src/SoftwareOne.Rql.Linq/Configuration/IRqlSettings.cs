#pragma warning disable IDE0130

namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSettings
    {
        MemberFlag DefaultMemberFlags { get; set; }
        IRqlSelectSettings Select { get; }
    }

    public interface IRqlSelectSettings
    {
        SelectMode ObjectMode { get; set; }
        SelectMode ReferenceMode { get; set; }
        int? MaxSelectDepth { get; set; }
    }
}
