namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators
{
    [AttributeUsage(AttributeTargets.Interface)]
    internal class ExpressionAttribute : Attribute
    {
        public ExpressionAttribute(Type key, Type implementation)
        {
            Key = key;
            Implementation = implementation;
        }

        public Type Key { get; init; }
        public Type Implementation { get; init; }
    }
}
