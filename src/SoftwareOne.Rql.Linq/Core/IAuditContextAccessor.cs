namespace SoftwareOne.Rql.Linq.Core
{
    internal interface IAuditContextAccessor
    {
        void SetContext(RqlAuditContext auditContext);
        void ReportOmittedPath(Func<string> setter);
    }
}
