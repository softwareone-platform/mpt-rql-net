using System.IO;

namespace SoftwareOne.Rql.Linq.Core
{
    internal class AuditContextAccessor : IAuditContextAccessor
    {
        private RqlAuditContext? _auditContext;

        public void SetContext(RqlAuditContext auditContext)
            => _auditContext = auditContext;

        public void ReportOmittedPath(Func<string> setter)
            => _auditContext?.Omitted.Add(setter());
    }
}
