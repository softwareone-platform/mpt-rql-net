﻿using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Order;

internal class OrderContext<T> : IOrderContext<T>, IOrderDefinitionProvider where T : class
{
    private IList<IOrderDefinition>? _orderDefinitions;

    public void AddOrder<TValue>(Expression<Func<T, TValue>> orderExpression, OrderDirection direction)
    {
        _orderDefinitions ??= new List<IOrderDefinition>();
        _orderDefinitions.Add(new OrderDefinition<T, TValue>(orderExpression, direction));
    }

    IList<IOrderDefinition>? IOrderDefinitionProvider.GetDefinition() => _orderDefinitions;
}