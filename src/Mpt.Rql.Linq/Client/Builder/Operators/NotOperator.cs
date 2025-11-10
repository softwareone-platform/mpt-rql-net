using Mpt.Rql.Client;

namespace Mpt.Rql.Linq.Client.Builder.Operators;

internal record NotOperator(IOperator Inner) : IOperator;