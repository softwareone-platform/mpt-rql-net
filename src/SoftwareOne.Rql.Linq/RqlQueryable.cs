using ErrorOr;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Mapping;
using SoftwareOne.Rql.Linq.Services.Ordering;
using SoftwareOne.Rql.Linq.Services.Projection;

namespace SoftwareOne.Rql.Linq
{
    internal class RqlQueryable<TStorage> : RqlQueryableLinq<TStorage, TStorage>, IRqlQueryable<TStorage>
    {
        public RqlQueryable(IMappingService<TStorage, TStorage> mapping,
            IFilteringService<TStorage> filter,
            IOrderingService<TStorage> order,
            IProjectionService<TStorage> projection)
            : base(mapping, filter, order, projection)
        {
        }
    }

    internal class RqlQueryableLinq<TStorage, TView> : IRqlQueryable<TStorage, TView>
    {
        private readonly IMappingService<TStorage, TView> _mapping;
        private readonly IFilteringService<TView> _filter;
        private readonly IOrderingService<TView> _order;
        private readonly IProjectionService<TView> _projection;

        public RqlQueryableLinq(
            IMappingService<TStorage, TView> mapping,
            IFilteringService<TView> filter,
            IOrderingService<TView> order,
            IProjectionService<TView> projection)
        {
            _mapping = mapping;
            _filter = filter;
            _order = order;
            _projection = projection;
        }

        public ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure)
        {
            var request = new RqlRequest();
            configure(request);
            return Transform(source, request);
        }

        public ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request)
        {
            var errors = new List<Error>();

            var query = _mapping.Apply(source);
            _filter.Apply(query, request.Filter).Switch(q => query = q, errors.AddRange);
            _order.Apply(query, request.Order).Switch(q => query = q, errors.AddRange);
            _projection.Apply(query, request.Select).Switch(q => query = q, errors.AddRange);

            return errors.Any() ? errors : ErrorOrFactory.From(query);
        }
    }
}
