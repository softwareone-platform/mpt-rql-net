#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

#pragma warning disable S2326
public interface IRqlRequestBuilderWithSelect<T> : IRqlRequestBuilderCommon where T : class
{
}