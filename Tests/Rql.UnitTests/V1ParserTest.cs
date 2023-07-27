using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Linear;

namespace Rql.UnitTests
{
    public class V1ParserTest : ParserTest
    {
        protected override IRqlParser GetParser()
            => new RqlParser();
    }
}