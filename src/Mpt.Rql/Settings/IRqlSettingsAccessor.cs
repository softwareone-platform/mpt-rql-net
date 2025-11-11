using Mpt.Rql.Abstractions.Configuration;

namespace Mpt.Rql.Settings;

internal interface IRqlSettingsAccessor
{
    IRqlSettings Current { get; }
}
