#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    [Flags]
    public enum RqlActions
    {
        None = 0,
        Select = 1 << 0,
        Filter = 1 << 1,
        Order = 1 << 2,

        All = Select | Filter | Order
    }
}
