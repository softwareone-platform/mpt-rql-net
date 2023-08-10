#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [Flags]
    public enum MemberFlag
    {
        None = 0,
        Reference = 1 << 0,
        Selectable = 1 << 1,
        Filterable = 1 << 2,
        Orderable = 1 << 3,

        Regular = Selectable | Filterable | Orderable,
        RegularAndReference = Regular | Reference
    }
}
