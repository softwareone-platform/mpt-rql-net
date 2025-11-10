#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IActionStrategy
{
    bool IsAllowed(RqlActions action);
}
