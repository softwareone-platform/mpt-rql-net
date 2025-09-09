namespace SoftwareOne.Rql.Abstractions.Configuration;

public class RqlMappingSettings
{
    /// <summary>
    /// When set to true RQL does not try to map source type to view type if they are the same.
    /// Note that enabling this feature may increase amount of data transferred between application and database server.
    /// </summary>
    public bool Transparent { get; set; }
}
