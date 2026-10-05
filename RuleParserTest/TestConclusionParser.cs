using System;
using Xunit;
using Rules;
using Rules.Parser;

namespace RuleParserTest
{
    public class TestConclusionParser
    {
        [Fact]
        public void TestConcusionExpr1()
        {
            string input = "Flag := smallNumber";
            RuleParser parser = new RuleParser(RulePart.Conclusion);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree == "Concl(AssignExpr(Id(Flag)Assign(:=)Expr(Term(Factor(Id(smallNumber))))))", "");
        }

        [Fact]
        public void TestConcusionExpr2()
        {
            string input = "smallNumber := length+5 * width - 12 / (Factor - 3.55E13)";
            RuleParser parser = new RuleParser(RulePart.Conclusion);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree ==
                 "Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook())))))", "");
        }

        [Fact]
        public void TestConcusionExpr3()
        {
            string input = "length+5 * width - 12 := smallNumber";
            RuleParser parser = new RuleParser(RulePart.Conclusion);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 4, "Syntax error(s) expected");
            Assert.True(parser.SyntaxErrors[0].Item1 ==
              "Syntax error at Row 0 Column 6: expected Identifier, Integer, Float, LeftHook but instead encountered \"+\"", "");
       }
    }
}
