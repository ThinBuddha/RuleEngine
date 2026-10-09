using Xunit;
using Rules.Parser;
using System.Globalization;
using Rules.Expression;

namespace RuleParserTest
{
    public class TestExpressionParser
    {
        [Fact]
        public void TestExpression1()
        {
            string input = "3 + 5 * 8 - 12";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            Assert.True(parser.ParseTree == "Expr(Term(Factor(Integer(3)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Integer(8)))MathOp(-)Term(Factor(Integer(12))))", "");
        }

        [Fact]
        public void TestExpression2()
        {
            string input = "(3 + 5) * 8 - 12";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            Assert.True(parser.ParseTree == "Expr(Term(Factor(Lhook()Expr(Term(Factor(Integer(3)))MathOp(+)Term(Factor(Integer(5))))Rhook())MathOp(*)Factor(Integer(8)))MathOp(-)Term(Factor(Integer(12))))", "");
        }

        [Fact]
        public void TestExpression3()
        {
            string input = "3 + 5 * (8 - 12)";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            Assert.True(parser.ParseTree ==
                "Expr(Term(Factor(Integer(3)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Lhook()Expr(Term(Factor(Integer(8)))MathOp(-)Term(Factor(Integer(12))))Rhook())))", "");
        }

        [Fact]
        public void TestExpression4()
        {
            string input = "3 + 5 * 8 - 12 / 56";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            Assert.True(parser.ParseTree == 
                "Expr(Term(Factor(Integer(3)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Integer(8)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Integer(56))))","");
        }

        [Fact]
        public void TestExpression5()
        {
            string input = "3+5 *8 - 12 / (56 - 3.55)";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            Assert.True(parser.ParseTree ==
                "Expr(Term(Factor(Integer(3)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Integer(8)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Integer(56)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))", "");
         }

        [Fact]
        public void TestExpression6()
        {
            string input = "length+5 * width - 12 / (Factor - 3.55)";
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            Assert.True(parser.ParseTree ==
                "Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))", "");
        }

        [Fact]
        public void TestExpression7()
        {
            // Testing expressions together with recovery, resulting in 2 valid expressions and 1 syntax error (due to the "and")
            string input = "length+5 * width - 12 / (Factor - 3.55) and 4 + 66";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 1 && 
                parser.SyntaxErrors[0].Item1 ==
                  "Syntax error at Row 0 Column 39: expected Identifier, Integer, Float, LeftHook, Minus but instead encountered \"and\"");
            
            Assert.True(parser.ParseTree ==
                "Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Expr(Term(Factor(Integer(4)))MathOp(+)Term(Factor(Integer(66))))", "");
        }

        [Fact]
        public void TestExpression8()
        {
            string input = "lenght * -39";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Expr(Term(Factor(Id(lenght))MathOp(*)Minus(Factor(Integer(39)))))", "");
        }

        [Fact]
        public void TestExpression9()
        {
            string input = "lenght * -(39/smallNumber)";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Expr(Term(Factor(Id(lenght))MathOp(*)Minus(Factor(Lhook()Expr(Term(Factor(Integer(39))MathOp(/)Factor(Id(smallNumber))))Rhook()))))", "");
        }

        [Fact]
        public void TestExpression10()
        {
            string input = "length+5 * width - 12 / (Factor - 3.55E13)";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            // Test parser
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree ==
                 "Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook())))", "");

            // Test builder
            Assert.True(parser.Builder.Result.GetType() == typeof(TExpression), "");
            TExpression expr = (TExpression)parser.Builder.Result;
            Assert.True(expr.Terms.Count == 3, "");
            Assert.True(expr.Operators.Count == 2, "");
            // TODO: decompose expr entirely....
            Assert.Equal("(length + (5 * width) - (12 / (Factor - 35500000000000)))", expr.ToString());
        }

        [Theory]
        [InlineData("en-US", "3.55", 3.55)]
        [InlineData("nl-NL", "3.55", 3.55)]
        [InlineData("en-US", "3.55E13", 35500000000000d)]
        [InlineData("nl-NL", "3.55E13", 35500000000000d)]
        public void FloatLiteralsUseTheRuleLanguageDecimalSeparator(string cultureName, string input, double expected)
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var parser = new RuleParser(RulePart.Expression);
                parser.Parse(input);

                Assert.Empty(parser.SyntaxErrors);
                var expression = Assert.IsType<TExpression>(parser.Builder.Result);
                var value = Assert.IsType<TConstantNumValue>(expression.Terms[0].Factors[0]);
                Assert.Equal(expected, value.Value.DoubleValue);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [Fact]
        public void TestExpression11()
        {
            string input = "-(39/smallNumber)";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Expr(Term(Minus(Factor(Lhook()Expr(Term(Factor(Integer(39))MathOp(/)Factor(Id(smallNumber))))Rhook()))))", "");
        }

        [Fact]
        public void TestExpression12()
        {
            string input = "-39 / smallNumber";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Expr(Term(Minus(Factor(Integer(39)))MathOp(/)Factor(Id(smallNumber))))", "");
        }

        [Fact]
        public void TestExpression13()
        {
            string input = "-(-4 * 50)";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Expr(Term(Minus(Factor(Lhook()Expr(Term(Minus(Factor(Integer(4)))MathOp(*)Factor(Integer(50))))Rhook()))))", "");

            Assert.True(parser.Builder.Result is TExpression);
            TExpression expr = (TExpression)parser.Builder.Result;
            Assert.True(expr.Terms[0].Factors.Count == 1, "Expression not correctly compiled, missing TFactor");
            Assert.Equal(200, ((TConstantNumValue)expr.Evaluate(null)).Value.IntValue);
        }

        [Theory]
        [InlineData("(-4 * 50)", -200)]
        [InlineData("-(4 * 50)", -200)]
        [InlineData("2 * -(4 + 1)", -10)]
        [InlineData("-(4 + 1) + 3", -2)]
        public void UnaryMinusBeforeParenthesesProducesAUsableFactor(string input, int expected)
        {
            var parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.Empty(parser.SyntaxErrors);
            var expression = Assert.IsType<TExpression>(parser.Builder.Result);
            Assert.Equal(expected, Assert.IsType<TConstantNumValue>(expression.Evaluate(null)).Value.IntValue);
        }
    }

}
