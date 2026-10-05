using Rules.Expression;
using Rules.Parser;
using System;
using Xunit;

namespace RuleTest
{
    public class TestExpressionPart
    {
        [Fact]
        public void TestExpressionAtom()
        {
            string input = "3";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            var expr = parser.Builder.Result as TExpression;
            TConstantValue result = expr.Evaluate(null);
            Assert.True((result as TConstantNumValue).Value.IntValue == 3);
        }
        public void TestExpression()
        {
            string input = "3 + 5 * 8 - 12";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            var expr = parser.Builder.Result as TExpression;
            TConstantValue result = expr.Evaluate(null);
            Assert.True((result as TConstantNumValue).Value.IntValue == 31);
        }

        [Fact]
        public void TestExpression2()
        {
            string input = "3 + 5 * (8 - 12)";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            var expr = parser.Builder.Result as TExpression;
            TConstantValue result = expr.Evaluate(null);
            Assert.True((result as TConstantNumValue).Value.IntValue == -17);
        }

        [Fact]
        public void TestExpression3()
        {
            string input = "3 * -12";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            var expr = parser.Builder.Result as TExpression;
            TConstantValue result = expr.Evaluate(null);
            Assert.True((result as TConstantNumValue).Value.IntValue == -36);
        }
    }
}
