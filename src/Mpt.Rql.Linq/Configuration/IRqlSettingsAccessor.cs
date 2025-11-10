using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Linq.Configuration;

internal interface IRqlSettingsAccessor
{
    IRqlSettings Current { get; }

    void Override(RqlSettings? settings);
}
