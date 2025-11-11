using Mpt.Rql;

namespace Rql.Tests.Common.Utility;

internal class MetadataOperatorTestEntity
{
    public string StringDefaults { get; set; } = null!;

    public int GenericDefaults { get; set; }

    public int? GenericNullableDefaults { get; set; }

    [RqlProperty(Operators = RqlOperators.Gt | RqlOperators.Lt)]
    public int GreaterThanLessThan { get; set; }

    public Guid GuidDefaults { get; set; }

    public Guid? NullableGuidDefaults { get; set; }

    [RqlProperty(Operators = RqlOperators.None)]
    public string None { get; set; } = null!;
}
