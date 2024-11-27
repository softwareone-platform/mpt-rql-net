using Rql.Tests.Integration.Tests.Functionality.Utility;

namespace Rql.Tests.Integration.Tests.Functionality;

public class CustomizedSelectTests : BasicSelectTests
{
    public CustomizedSelectTests()
    {
        TestExecutor = new CustomizedShapeTestExecutor();
    }
}