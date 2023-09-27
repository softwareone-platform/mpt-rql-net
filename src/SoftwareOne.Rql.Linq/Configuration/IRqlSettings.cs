#pragma warning disable IDE0130

namespace SoftwareOne.Rql.Linq.Configuration
{
    public interface IRqlSettings
    {
        RqlActions DefaultActions { get; set; }
        RqlOperators AllowedOperators { get; set; }
        IRqlSelectSettings Select { get; }
    }
}
