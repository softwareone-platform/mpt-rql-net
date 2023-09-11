using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Operators;

internal record NotOperator(IOperator Inner) : IOperator;