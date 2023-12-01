namespace SoftwareOne.Rql.Linq.Core
{
    internal class AuditContextAccessor : IAuditContextAccessor
    {
        private RqlAuditContext? _auditContext;

        public void SetContext(RqlAuditContext auditContext)
            => _auditContext = auditContext;

        public void ReportOmittedPath(Func<string> setter)
            => _auditContext?.Omitted.Add(setter());

        public void ReportInvisiblePath(Func<string> setter)
            => _auditContext?.Invisible.Add(setter());
    }
}
