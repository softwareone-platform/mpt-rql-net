namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators
{
    internal interface IOperatorHandlerMaper
    {
        bool ContainsKey(Type key);
        void Add(Type key, Type value);
        bool TryGetValue(Type key, out Type? value);
    }
}
