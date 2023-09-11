﻿using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Request
{
    #pragma warning disable S3444
    internal interface IRqlRequestBuilderContext<T> : IRqlRequestBuilder<T>, IFilteredRqlRequestBuilder<T>, IOrderedRqlRequestBuilder<T> where T : class
    {
    }
}
