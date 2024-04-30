namespace SoftwareOne.Rql.Linq.Configuration
{
    internal class RqlSelectSettings : IRqlSelectSettings
    {
        public RqlSelectModes Implicit { get; set; }
        
        public RqlSelectModes Explicit { get; set; }

        public int? MaxDepth { get; set; }
    }
}
