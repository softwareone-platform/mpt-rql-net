﻿using SoftwareOne.Rql.Linq.Core.Result;

namespace SoftwareOne.Rql.Linq.Services.Context;

internal interface IQueryContext<TView>
{
    IEnumerable<Error> GetErrors();
    void AddTransformation(Func<IQueryable<TView>, IQueryable<TView>> func);
    IQueryable<TView> ApplyTransformations(IQueryable<TView> query);
    void AddError(Error error);
    void AddErrors(IEnumerable<Error> errors);
    public bool HasErrors { get; }
    RqlNode Graph { get; }
}