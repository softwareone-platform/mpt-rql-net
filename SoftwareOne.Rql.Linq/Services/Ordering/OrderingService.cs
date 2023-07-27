using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Ordering
{
    internal sealed class OrderingService<TView> : RqlService, IOrderingService<TView>
    {
        private readonly IRqlParser _parser;

        public OrderingService(ITypeMetadataProvider typeNameMaper, IRqlParser parser) : base(typeNameMaper)
        {
            _parser = parser;
        }

        protected override string ErrorPrefix => "order";

        public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? order)
        {
            if (string.IsNullOrEmpty(order))
                return ErrorOrFactory.From(query);

            var node = _parser.Parse(order);

            var orderProps = node.Items!.OfType<RqlConstant>().ToList();
            if (!orderProps.Any())
                return Error.Validation(MakeErrorCode("no_props"), "No valid ordering properties were detected");

            bool isFirst = true;
            var param = Expression.Parameter(typeof(TView));

            var errors = new List<Error>();

            foreach (var op in orderProps)
            {
                var (path, isAsc) = StringHelper.ExtractSign(op.Value);

                var eoMemberAccess = MakeMemberAccess(param, path.ToString(), path =>
                {
                    if (!path.PropertyInfo.Flags.HasFlag(MemberFlag.AllowOrder))
                        return Error.Validation(MakeErrorCode(path.Path.ToString()), "Ordering is not permitted.");
                    return Result.Success;
                });

                if (eoMemberAccess.IsError)
                {
                    errors.AddRange(eoMemberAccess.Errors);
                    continue;
                }

                var functions = (IOrderingFunctions)Activator.CreateInstance(typeof(OrderingFunctions<,>).MakeGenericType(typeof(TView), eoMemberAccess.Value.Type))!;

                MethodInfo mi;
                if (isAsc)
                    mi = isFirst ? functions.GetOrderBy() : functions.GetThenBy();
                else
                    mi = isFirst ? functions.GetOrderByDescending() : functions.GetThenByDescending();

                var orderExp = Expression.Lambda(eoMemberAccess.Value, param);

                query = (IQueryable<TView>)mi.Invoke(null, new object[] { query, orderExp })!;
                isFirst = false;
            }

            return errors.Any() ? errors : ErrorOrFactory.From(query);
        }
    }
}
