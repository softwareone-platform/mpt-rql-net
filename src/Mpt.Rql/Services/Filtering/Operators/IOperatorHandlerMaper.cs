namespace Mpt.Rql.Services.Filtering.Operators;

internal interface IOperatorHandlerMapper
{
    bool ContainsKey(Type key);
    void Add(Type key, Type value);
    bool TryGetValue(Type key, out Type? value);
}