﻿using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Operators;

internal record Out<T, U>(Expression<Func<T, U>> Exp, IEnumerable<U> Values) : MultiComparableOperator<T, U>(Exp, Values) where T : class;