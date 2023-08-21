using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Parsers.Linear;

// TODO: BS 18/08/2023 This class needs to be further refactored, separated it out for now but will come back to this VERY SOON
internal class RqlExpressionReducer
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
                currentGroup.AddLast(item.Expression);
            }
            else
            {
                // AND -> OR
                if (item.Type == GroupType.Or)
                {
                    if (currentGroup.Count > 1)
                    {
                        var andExp = RqlExpression.And(currentGroup);
                        orGroups.AddLast(andExp);
                        currentGroup.Clear();
                    }

                    currentGroup.AddLast(item.Expression);
                }
                // OR -> AND
                else
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
                }
            }

            currentType = item.Type;
        }


        // transfer remaining items from current group
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