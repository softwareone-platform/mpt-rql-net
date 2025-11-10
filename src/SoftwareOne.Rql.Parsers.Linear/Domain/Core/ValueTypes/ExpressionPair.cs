using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

internal record struct ExpressionPair(GroupType Type, RqlExpression Expression);
