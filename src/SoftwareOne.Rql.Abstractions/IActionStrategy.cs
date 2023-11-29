#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IActionStrategy
{
    bool IsAllowed(RqlActions action);
}
