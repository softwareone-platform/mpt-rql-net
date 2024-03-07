namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlDefaultSettings : IRqlDefaultSettings
    {
        public IRqlGeneralSettings General { get; init; } = null!;
        public IRqlSelectSettings Select { get; init; } = null!;
    }
}
