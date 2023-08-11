#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [Flags]
    public enum RqlAction
    {
        None = 0,
        Select = 1 << 1,
        Filter = 1 << 2,
        Order = 1 << 3,

        All = Select | Filter | Order,
    }
}
