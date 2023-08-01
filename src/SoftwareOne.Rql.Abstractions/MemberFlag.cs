#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [Flags]
    public enum MemberFlag
    {
        None = 0,
        IsDefault = 1 << 0,
        AllowFilter = 1 << 1,
        AllowOrder = 1 << 2,

        All = ~0
    }
}
