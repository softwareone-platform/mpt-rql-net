using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Linq.Settings;

internal interface IRqlSettingsAccessor
{
    IRqlSettings Current { get; }
}
