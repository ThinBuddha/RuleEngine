using System;
using Xunit;
using Rules.Parser;
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
            Assert.True(expr.ToString() == "(length + (5 * width) - (12 / (Factor - 35500000000000)))", "");
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
            // TODO: dit maakt een TExpression aan met daarin 1 Term die leeg is...dus evaluatie levert uiteraard assertion op in TTerm.Evaluate()
            // waarom maakt ie in de TTerm geen TFactor aan?
            // Bijv. "(-4 * 50)" gaat wel goed, maar "-(4 * 50) ook niet; het zit dus in het patroon "-("...
            // Expr(Term(Minus(Factor(LHOOK()....Op de een of andere manier haalt die LHook() de hele daar bovenliggende Factor( weg! 
            // => It seems like ParseTree.Add(LHook) should also add something in the builder stack, right now it is skipped by the builder...?
            string input = "-(-4 * 50)";

            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Expr(Term(Minus(Factor(Lhook()Expr(Term(Minus(Factor(Integer(4)))MathOp(*)Factor(Integer(50))))Rhook()))))", "");

            Assert.True(parser.Builder.Result is TExpression);
            TExpression expr = (TExpression)parser.Builder.Result;
            Assert.True(expr.Terms[0].Factors.Count == 1, "Expression not correctly compiled, missing TFactor");
        }
    }

}
