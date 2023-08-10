﻿namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators
{
    internal class OperatorHandlerMapper : IOperatorHandlerMapper
    {
        private readonly Dictionary<Type, Type> _mapping;

        public OperatorHandlerMapper()
        {
            _mapping = new Dictionary<Type, Type>();
        }

        public bool ContainsKey(Type key) => _mapping.ContainsKey(key);

        public void Add(Type key, Type value) => _mapping.Add(key, value);

        public bool TryGetValue(Type key, out Type? value) => _mapping.TryGetValue(key, out value);
    }
}
