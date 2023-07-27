using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;

namespace Rql.UnitTests
{
    public abstract class ParserTest
    {
        protected abstract IRqlParser GetParser();

        [Theory]
        [InlineData("status=processing&limit=2&select=(name,id)&order=mail,-name")]
        public void TestFullQueryEqual(string query)
        {
            var op = GetParser().Parse(query);

            var eq = Assert.IsType<RqlEqual>(op.Items![0]);
            var member = Assert.IsType<RqlConstant>(eq.Left);
            Assert.Equal("status", member.Value);
            var constant = Assert.IsType<RqlConstant>(eq.Right);
            Assert.Equal("processing", constant.Value);
        }


        [Theory]
        [InlineData("status=processing")]
        [InlineData("status=eq=processing")]
        [InlineData("eq(status,processing)")]
        public void TestEqual(string query)
        {
            var op = GetParser().Parse(query);

            var eq = Assert.IsType<RqlEqual>(op.Items![0]);
            var member = Assert.IsType<RqlConstant>(eq.Left);
            Assert.Equal("status", member.Value);
            var constant = Assert.IsType<RqlConstant>(eq.Right);
            Assert.Equal("processing", constant.Value);
        }


        [Theory]
        [InlineData("gt(events.created.at,2020-01-01T00:00:00+00:00)")]
        [InlineData("events.created.at=gt=2020-01-01T00:00:00+00:00")]
        public void TestDate(string query)
        {
            var parsed = GetParser().Parse(query);

            var op = Assert.IsType<RqlGreaterThan>(parsed.Items![0]);
            var member = Assert.IsType<RqlConstant>(op.Left);
            Assert.Equal("events.created.at", member.Value);
            var constant = Assert.IsType<RqlConstant>(op.Right);
            Assert.Equal("2020-01-01T00:00:00+00:00", constant.Value);
        }

        [Theory]
        [InlineData("like(product.name,*best*)")]
        public void TestLike(string query)
        {
            var parsed = GetParser().Parse(query);

            var op = Assert.IsType<RqlLike>(parsed.Items![0]);
            var member = Assert.IsType<RqlConstant>(op.Left);
            Assert.Equal("product.name", member.Value);
            var constant = Assert.IsType<RqlConstant>(op.Right);
            Assert.Equal("*best*", constant.Value);
        }

        [Theory]
        [InlineData("in(status,(processing,active,closed,created,new))")]
        public void TestArray(string query)
        {
            var parsed = GetParser().Parse(query);

            var op = Assert.IsType<RqlListIn>(parsed.Items![0]);

            Assert.Equal("status", Assert.IsType<RqlConstant>(op.Left).Value);
         
            var list = Assert.IsAssignableFrom<RqlGroup>(op.Right).Items!.OfType<RqlConstant>().ToList();
            
            Assert.Equal("processing", list[0].Value);
            Assert.Equal("active", list[1].Value);
            Assert.Equal("closed", list[2].Value);
            Assert.Equal("created", list[3].Value);
            Assert.Equal("new", list[4].Value);
        }

        [Theory]
        [InlineData("in(status,(active))")]
        public void TestSinglleArray(string query)
        {
            var parsed = GetParser().Parse(query);
            var op = Assert.IsType<RqlListIn>(parsed.Items![0]);
            Assert.Equal("status", Assert.IsType<RqlConstant>(op.Left).Value);
            var list = Assert.IsAssignableFrom<RqlGroup>(op.Right).Items!.OfType<RqlConstant>().ToList();
            Assert.Equal("active", list[0].Value);
        }

        [Theory]
        [InlineData("id=PRD-0000-0001&status=active")]
        [InlineData("and(id=PRD-0000-0001,status=active)")]
        [InlineData("id=PRD-0000-0001,status=active")]
        [InlineData("id=PRD-0000-0001,eq(status,active)")]
        [InlineData("and(eq(id,PRD-0000-0001),eq(status,active))")]
        public void TestAnd(string query)
        {
            var op = GetParser().Parse(query);

            var and = Assert.IsType<RqlAnd>(op);

            CheckListOfConstantComparisons<RqlEqual>(and.Items!, 0, "id", "PRD-0000-0001");
            CheckListOfConstantComparisons<RqlEqual>(and.Items!, 1, "status", "active");
        }

        [Theory]
        [InlineData("id=null()")]
        public void TestNull(string query)
        {
            var op = GetParser().Parse(query);

            var grp = Assert.IsAssignableFrom<RqlGroup>(op);
            var eq = Assert.IsType<RqlEqual>(grp.Items![0]);
            Assert.IsType<RqlConstant>(eq.Left);
            Assert.IsType<RqlNull>(eq.Right);
        }

        [Theory]
        [InlineData("id=empty()")]
        public void TestEmpty(string query)
        {
            var op = GetParser().Parse(query);

            var grp = Assert.IsAssignableFrom<RqlGroup>(op);
            var eq = Assert.IsType<RqlEqual>(grp.Items![0]);
            Assert.IsType<RqlConstant>(eq.Left);
            Assert.IsType<RqlEmpty>(eq.Right);
        }

        [Theory]
        [InlineData("id=PRD-0000-0001&status=active|mode=1&mode=2")]
        [InlineData("(id=PRD-0000-0001&status=active)|mode=1&mode=2")]
        [InlineData("id=PRD-0000-0001&status=active|(mode=1&mode=2)")]
        [InlineData("(id=PRD-0000-0001&status=active)|(mode=1&mode=2)")]
        [InlineData("((id=PRD-0000-0001&status=active)|(mode=1&mode=2))")]
        public void TestAndOr(string query)
        {
            var op = GetParser().Parse(query);

            var or = Assert.IsType<RqlOr>(op);
            var items = or.Items!.ToList();

            var and1 = Assert.IsType<RqlAnd>(items[0]);

            CheckListOfConstantComparisons<RqlEqual>(and1.Items!, 0, "id", "PRD-0000-0001");
            CheckListOfConstantComparisons<RqlEqual>(and1.Items!, 1, "status", "active");

            var and2 = Assert.IsType<RqlAnd>(items[1]);
            CheckListOfConstantComparisons<RqlEqual>(and2.Items!, 0, "mode", "1");
            CheckListOfConstantComparisons<RqlEqual>(and2.Items!, 1, "mode", "2");
        }

        [Theory]
        [InlineData("id=PRD-0000-0001|status=active")]
        [InlineData("or(id=PRD-0000-0001,status=active)")]
        [InlineData("id=PRD-0000-0001;status=active")]
        [InlineData("or(eq(id,PRD-0000-0001),eq(status,active))")]
        public void TestOrd(string query)
        {
            var op = GetParser().Parse(query);

            var or = Assert.IsType<RqlOr>(op);

            CheckListOfConstantComparisons<RqlEqual>(or.Items!, 0, "id", "PRD-0000-0001");
            CheckListOfConstantComparisons<RqlEqual>(or.Items!, 1, "status", "active");
        }

        [Theory]
        [InlineData("product.name='white space & special^ symbols!'")]
        [InlineData("eq(product.name,'white space & special^ symbols!')")]
        public void Test_SpecialChars_Ampersamp(string query)
        {
            var op = GetParser().Parse(query);

            var eq = Assert.IsType<RqlEqual>(op.Items![0]);
            var member = Assert.IsType<RqlConstant>(eq.Left);
            Assert.Equal("product.name", member.Value);
            var constant = Assert.IsType<RqlConstant>(eq.Right);
            Assert.Equal("white space & special^ symbols!", constant.Value);
        }

        [Theory]
        [InlineData(@"product.name='i am ""happy"" is quoted here'")]
        [InlineData(@"product.name=eq='i am ""happy"" is quoted here'")]
        [InlineData(@"eq(product.name,'i am ""happy"" is quoted here')")]
        public void Test_SpecialChars_Quote(string query)
        {
            var op = GetParser().Parse(query);

            var eq = Assert.IsType<RqlEqual>(op.Items![0]);
            var member = Assert.IsType<RqlConstant>(eq.Left);
            Assert.Equal("product.name", member.Value);
            var constant = Assert.IsType<RqlConstant>(eq.Right);
            Assert.Equal(@"i am ""happy"" is quoted here", constant.Value);
        }

        [Fact]
        public void TestInFunctionPrefix3()
        {
            var op = GetParser().Parse("status=eq=processing");

            var eq = Assert.IsType<RqlEqual>(op.Items![0]);
            var member = Assert.IsType<RqlConstant>(eq.Left);
            Assert.Equal("status", member.Value);
            var constant = Assert.IsType<RqlConstant>(eq.Right);
            Assert.Equal("processing", constant.Value);
        }

        [Fact]
        public void TestSelectStatement()
        {
            var op = GetParser().Parse("id,sub(*,-id),-id");

            var grp = Assert.IsAssignableFrom<RqlGroup>(op);

            var item1 = Assert.IsType<RqlConstant>(grp.Items![0]);
            Assert.Equal("id", item1.Value);

            var item2 = Assert.IsType<RqlGenericGroup>(grp.Items[1]);
            Assert.Equal("sub", item2.Name);

            Assert.Equal("*", Assert.IsType<RqlConstant>(item2.Items![0]).Value);
            Assert.Equal("-id", Assert.IsType<RqlConstant>(item2.Items[1]).Value);
         
            Assert.Equal("-id", Assert.IsType<RqlConstant>(grp.Items[2]).Value);
        }


        private static void CheckListOfConstantComparisons<TComparison>(IReadOnlyList<RqlExpression> items, int index, string prop, string value)
            where TComparison : RqlBinary
        {
            var item = Assert.IsType<TComparison>(items[index]);
            var itemMember = Assert.IsType<RqlConstant>(item.Left);
            var itemConst = Assert.IsType<RqlConstant>(item.Right);
            Assert.Equal(prop, itemMember.Value);
            Assert.Equal(value, itemConst.Value);
        }

    }
}