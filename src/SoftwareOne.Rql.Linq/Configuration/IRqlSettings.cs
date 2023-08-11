#pragma warning disable IDE0130

namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSettings
    {
        RqlAction DefaultActions { get; set; }
        IRqlSelectSettings Select { get; }
    }
}
