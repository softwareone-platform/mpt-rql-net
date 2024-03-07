#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [Flags]
    public enum RqlSelectModes
    {
        None = 0,
        Core = 1 << 0,
        Primitive = 1 << 1,
        Reference = 1 << 2,
        Collection = 1 << 3,
        All = Primitive | Core | Reference | Collection,
    }
}
