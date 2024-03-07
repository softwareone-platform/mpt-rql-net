namespace SoftwareOne.Rql.Linq.Configuration
{
    internal interface IRqlDefaultSettings
    {
        IRqlGeneralSettings General { get; }
        IRqlSelectSettings Select { get; }
    }
}
