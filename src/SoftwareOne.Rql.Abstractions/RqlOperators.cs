#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [Flags]
    public enum RqlOperators
    {
        None = 0,
        Eq = 1 << 0,
        Ne = 1 << 1,
        Gt = 1 << 2,
        Ge = 1 << 3,
        Lt = 1 << 4,
        Le = 1 << 5,
        ListIn = 1 << 6,
        ListOut = 1 << 7,
        StartsWith = 1 << 8,
        Contains = 1 << 9,
        EndsWith = 1 << 10,
        Null = 1 << 11,
        Empty = 1 << 12,
        Any = 1 << 13,
        All = 1 << 14,

        GenericDefaults = Eq | Ne | Gt | Ge | Lt | Le | ListIn | ListOut,
        GuidDefaults = Eq | Ne | ListIn | ListOut,
        StringDefaults = Eq | Ne | StartsWith | EndsWith | Contains | ListIn | ListOut | Empty | Null,
        CollectionDefaults = Any | All,

        AllOperators = Eq | Ne | Gt | Ge | Lt | Le | ListIn | ListOut | StartsWith | Contains | EndsWith | Null | Empty | All | Any
    }
}
