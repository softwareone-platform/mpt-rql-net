using Mpt.Rql.Client;

namespace Mpt.Rql.Client.Builder.Operators;

internal record NotOperator(IOperator Inner) : IOperator;