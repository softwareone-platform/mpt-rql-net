using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Parsers.Linear;

namespace Rql.Tests.Unit
{
    public class V1ParserTest : ParserTest
    {
        protected override IRqlParser GetParser()
            => new RqlParser();
    }
}