using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Declarative;

namespace Rql.UnitTests
{
    public class V2ParserTest : ParserTest
    {
        protected override IRqlParser GetParser()
            => new DeclarativeParser();
    }
}