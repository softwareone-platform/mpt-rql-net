using Mpt.Rql.Abstractions;
using Mpt.Rql.Parsers.Linear.Core.Enumerations;

namespace Mpt.Rql.Parsers.Linear.Core.ValueTypes;

internal record struct ExpressionPair(GroupType Type, RqlExpression Expression);
