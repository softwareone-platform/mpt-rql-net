using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;

namespace SoftwareOne.Rql.Parsers.Linear;

/// <summary>
/// This class is responsible for reducing multiple RqlAnd and RqlOr RqlExpressions down to a single RqlExpressions
/// This is easier to explain via an example e.g. take the following query example: 
///     field1=value1&field2=value2|field3=value3&field4=value4
///     
/// This query would be the input into this class as a list of the following expressions:
///     GroupType=None      Type=RqlEqual(RqlConstant(field1), RqlConstant(value1))
///     GroupType=And       Type=RqlEqual(RqlConstant(field2), RqlConstant(value2))
///     GroupType=Or        Type=RqlEqual(RqlConstant(field3), RqlConstant(value3))
///     GroupType=And       Type=RqlEqual(RqlConstant(field4), RqlConstant(value4))
///     
/// However, the reduced result would be a single RqlOr result:
/// RqlOr 
///     RqlAnd
///         Type=RqlEqual(RqlConstant(field1), RqlConstant(value1))
///         Type=RqlEqual(RqlConstant(field2), RqlConstant(value2))
///     RqlAnd
///         Type=RqlEqual(RqlConstant(field3), RqlConstant(value3))
///         Type=RqlEqual(RqlConstant(field4), RqlConstant(value4))
/// </summary>
internal static class RqlExpressionReducer
{
    public static RqlExpression Reduce(IList<ExpressionPair> expressions)
    {
        var orGroups = new LinkedList<RqlExpression>();
        var currentGroup = new LinkedList<RqlExpression>();
        GroupType? currentType = null;

        foreach (var item in expressions)
        {
            if (!currentType.HasValue || item.Type == currentType)
            {
                ProcessForNoTransition(currentGroup, item, ref currentType);
            }
            else if (item.Type == GroupType.Or)
            {
                // If we stop processing GroupType 'And' start processing GroupType 'Or', we create a single 'And' RqlExpression of currentGroup
                // and clear currentGroup to start processing 'Or' RqlExpressions
                ProcessTransitionToGroupTypeOr(currentGroup, orGroups, item, ref currentType);
            }
            else
            {
                // If we stop processing GroupType 'Or' start processing GroupType 'And', we add alls RqlExpressions of currentGroup to orGroup
                // Note, orGroup is reduced to a single RqlOr RqlExpression later on
                ProcessTransitionToGroupTypeAnd(currentGroup, orGroups, item, ref currentType);
            }
        }

        // Reduce to single expression, reducing RqlAnd and RqlOr RqlExpressions if necessary
        return ReduceToSingleExpression(currentGroup, orGroups, currentType);
    }

    private static void ProcessForNoTransition(
      LinkedList<RqlExpression> currentGroup,
      ExpressionPair item,
      ref GroupType? currentType)
    {
        currentGroup.AddLast(item.Expression);

        currentType = item.Type;
    }

    private static void ProcessTransitionToGroupTypeOr(
       LinkedList<RqlExpression> currentGroup,
       LinkedList<RqlExpression> orGroups,
       ExpressionPair item,
       ref GroupType? currentType)
    {
        if (currentGroup.Count > 1)
        {
            var andExp = RqlExpression.And(currentGroup);
            orGroups.AddLast(andExp);
            currentGroup.Clear();
        }

        currentGroup.AddLast(item.Expression);

        currentType = item.Type;
    }

    private static void ProcessTransitionToGroupTypeAnd(
        LinkedList<RqlExpression> currentGroup,
        LinkedList<RqlExpression> orGroups,
        ExpressionPair item,
        ref GroupType? currentType)
    {
        var prev = currentGroup.Last;
        if (prev != null)
            currentGroup.RemoveLast();

        foreach (var or in currentGroup)
            orGroups.AddLast(or);

        currentGroup.Clear();
        if (prev != null)
            currentGroup.AddLast(prev);
        currentGroup.AddLast(item.Expression);

        currentType = item.Type;
    }

    private static RqlExpression ReduceToSingleExpression(
        LinkedList<RqlExpression> currentGroup,
        LinkedList<RqlExpression> orGroups,
        GroupType? currentType)
    {
        if (currentGroup.Count > 1 && currentType == GroupType.And)
        {
            orGroups.AddLast(RqlExpression.And(currentGroup));
        }
        else
        {
            foreach (var or in currentGroup)
                orGroups.AddLast(or);
        }

        if (orGroups.Count == 1)
            return orGroups.First!.Value;

        return RqlExpression.Or(orGroups);
    }
}