using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List
{
    [Expression(typeof(RqlListOut), typeof(ListOut))]
    public interface IListOut : IListOperator { }
}
