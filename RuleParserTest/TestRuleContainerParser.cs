using Rules;
using Rules.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace RuleParserTest
{
    public class TestRuleContainerParser
    {
        [Fact]
        public void TestRuleContainer()
        {
            string input =
                "RuleContainer Model.Stuff.MyContainer" + Environment.NewLine +
                Environment.NewLine +
                "  RULE myComplexRule " + Environment.NewLine +
                "  IF length+5 * width - 12 / (Factor - 3.55) > smallNumber " + Environment.NewLine +
                "  AND IsTrueOrMaybeNot" + Environment.NewLine +
                "  THEN smallNumber := length+5 * width - 12 / (Factor - 3.55E13)" + Environment.NewLine +
                "  AND IsNotTrueAfterAll" + Environment.NewLine
                + Environment.NewLine +
                "  RULE my.second.Rule " + Environment.NewLine +
                "  IF p.length+5 - 3.55 > smallNumber " + Environment.NewLine +
                "  AND IsTrueOrMaybeNot" + Environment.NewLine +
                "  THEN Result.IsNotTrueAfterAll" + Environment.NewLine + Environment.NewLine;

            RuleParser parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "RuleCont(Id(Model.Stuff.MyContainer)Rule(Id(myComplexRule)If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Factor(Id(IsTrueOrMaybeNot))))))Then()Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook()))))LogicalOp(AND)AssignExpr(Id(IsNotTrueAfterAll))))Rule(Id(my.second.Rule)If()Cond(CompExpr(Expr(Term(Factor(Id(p.length)))MathOp(+)Term(Factor(Integer(5)))MathOp(-)Term(Factor(Float(3.55))))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Factor(Id(IsTrueOrMaybeNot))))))Then()Concl(AssignExpr(Id(Result.IsNotTrueAfterAll)))))", "");
        }

        [Fact]
        public void TestTwoRuleContainers()
        {
            string input =
                "RuleContainer Model.Stuff.MyContainer" + Environment.NewLine +
                Environment.NewLine +
                "  RULE myComplexRule " + Environment.NewLine +
                "  IF length+5 * width - 12 / (Factor - 3.55) > smallNumber " + Environment.NewLine +
                "  AND IsTrueOrMaybeNot" + Environment.NewLine +
                "  THEN smallNumber := length+5 * width - 12 / (Factor - 3.55E13)" + Environment.NewLine +
                "  AND IsNotTrueAfterAll" + Environment.NewLine
                + Environment.NewLine +
                "  RULE my.second.Rule " + Environment.NewLine +
                "  IF p.length+5 - 3.55 > smallNumber " + Environment.NewLine +
                "  AND IsTrueOrMaybeNot" + Environment.NewLine +
                "  THEN Result.IsNotTrueAfterAll" + Environment.NewLine + Environment.NewLine +
               "RuleContainer Model.Stuff.MyContainer" + Environment.NewLine +
               Environment.NewLine +
                "  RULE my.Other.ComplexRule " + Environment.NewLine +
                "  IF length + width - 12 > 8" + Environment.NewLine +
                "  THEN smallNumber := 10" + Environment.NewLine +
                Environment.NewLine +
                "  RULE my.Other.second.Rule " + Environment.NewLine +
                "  IF p.length+5 - 3.55 > smallNumber " + Environment.NewLine +
                "  AND NOT IsTrueOrMaybeNot" + Environment.NewLine +
                "  THEN Result.IsNotTrueAfterAll" + Environment.NewLine + Environment.NewLine;

            RuleParser parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "RuleCont(Id(Model.Stuff.MyContainer)Rule(Id(myComplexRule)If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Factor(Id(IsTrueOrMaybeNot))))))Then()Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook()))))LogicalOp(AND)AssignExpr(Id(IsNotTrueAfterAll))))Rule(Id(my.second.Rule)If()Cond(CompExpr(Expr(Term(Factor(Id(p.length)))MathOp(+)Term(Factor(Integer(5)))MathOp(-)Term(Factor(Float(3.55))))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Factor(Id(IsTrueOrMaybeNot))))))Then()Concl(AssignExpr(Id(Result.IsNotTrueAfterAll)))))RuleCont(Id(Model.Stuff.MyContainer)Rule(Id(my.Other.ComplexRule)If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))))Compare(>)Expr(Term(Factor(Integer(8))))))Then()Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Integer(10)))))))Rule(Id(my.Other.second.Rule)If()Cond(CompExpr(Expr(Term(Factor(Id(p.length)))MathOp(+)Term(Factor(Integer(5)))MathOp(-)Term(Factor(Float(3.55))))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Not(Factor(Id(IsTrueOrMaybeNot)))))))Then()Concl(AssignExpr(Id(Result.IsNotTrueAfterAll)))))", "");
        }
    }
}
