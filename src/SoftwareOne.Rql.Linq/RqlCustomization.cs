#pragma warning disable IDE0130
using SoftwareOne.Rql.Linq.Configuration;

namespace SoftwareOne.Rql;

public class RqlCustomization
{
    public IRqlSelectSettings? Select { get; set; }
}