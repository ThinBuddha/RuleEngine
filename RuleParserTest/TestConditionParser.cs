using System;
using Xunit;
using Rules;
using Rules.Parser;

namespace RuleParserTest
{
    public class TestConditionParser
    {
        [Fact]
        public void TestOneMathExpr()
        {
            string input = "length+5 * width - 12 / (Factor - 3.55) > smallNumber";
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree ==
                 "Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber))))))", "");
        }

        [Fact]
        public void TestThreeMathExprs()
        {
            string input = "length+5 * width - 12 / (Factor - 3.55) > smallNumber and 4 + 66 <= bigNumber - 9 and 8 < smallNumber";
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree ==
                 "Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(and)CompExpr(Expr(Term(Factor(Integer(4)))MathOp(+)Term(Factor(Integer(66))))Compare(<=)Expr(Term(Factor(Id(bigNumber)))MathOp(-)Term(Factor(Integer(9)))))LogicalOp(and)CompExpr(Expr(Term(Factor(Integer(8))))Compare(<)Expr(Term(Factor(Id(smallNumber))))))", "");
        }

        [Fact]
        public void TestNoORInCondition()
        {
            // No "OR" in a rule condition
            string input = "not (a or b) = true";
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count > 0 && parser.SyntaxErrors[0].Item1 ==
               "Syntax error at Row 0 Column 7: expected )");
        }

        [Fact]
        public void TestOneLogicalExpr()
        {
            string input = "not a = true";
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree ==
                 "Cond(CompExpr(Expr(Term(Not(Factor(Id(a)))))Compare(=)Expr(Term(Factor(Id(true))))))", "");
        }

        [Fact]
        public void TestOneLogicalExprSimplified()
        {
            string input = "not a";
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree == "Cond(CompExpr(Expr(Term(Not(Factor(Id(a)))))))", "");
        }

        [Fact]
        public void TestThreeLogicalExprs()
        {
            string input = "not a and b and c";
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree == "Cond(CompExpr(Expr(Term(Not(Factor(Id(a))))))LogicalOp(and)CompExpr(Expr(Term(Factor(Id(b)))))LogicalOp(and)CompExpr(Expr(Term(Factor(Id(c))))))", "");
        }

        [Fact]
        public void TestThreeCombinedExprs()
        {
            string input = "not a and 8*b < smallNumber -1 and c";
            RuleParser parser = new RuleParser(RulePart.Condition);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Syntax error(s) not expected");
            Assert.True(parser.ParseTree == "Cond(CompExpr(Expr(Term(Not(Factor(Id(a))))))LogicalOp(and)CompExpr(Expr(Term(Factor(Integer(8))MathOp(*)Factor(Id(b))))Compare(<)Expr(Term(Factor(Id(smallNumber)))MathOp(-)Term(Factor(Integer(1)))))LogicalOp(and)CompExpr(Expr(Term(Factor(Id(c))))))", "");
        }
    }
}
