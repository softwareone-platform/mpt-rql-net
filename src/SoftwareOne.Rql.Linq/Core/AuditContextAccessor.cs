namespace SoftwareOne.Rql.Linq.Core
{
    internal class AuditContextAccessor : IAuditContextAccessor
    {
        private RqlAuditContext? _auditContext;
        private readonly HashSet<RqlPropertyInfo> _visitedPaths = new();

        public void SetContext(RqlAuditContext auditContext)
            => _auditContext = auditContext;

        public void ReportOmittedPath(Func<string> setter)
            => _auditContext?.Omitted.Add(setter());

        public void ReportInvisiblePath(Func<string> setter)
            => _auditContext?.Invisible.Add(setter());

        public bool IsCircularReference(RqlPropertyInfo rqlProperty)
        {
            if (_visitedPaths.Contains(rqlProperty)) return true;
            _visitedPaths.Add(rqlProperty);
            return false;
        }
    }
}
