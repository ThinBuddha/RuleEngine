using System;
using Xunit;
using Rules;
using Rules.Parser;

namespace RuleParserTest
{
    public class TestRuleParser
    {
        [Fact]
        public void TestOneRuleOneLine()
        {
           /* All in one line:
            * Rule myrule 
            * if length + 5 * width - 12 / (Factor - 3.55) > smallNumber 
            * then smallNumber := length + 5 * width - 12 / (Factor - 3.55E13)
            */
            string input = "Rule myrule if length+5 * width - 12 / (Factor - 3.55) > smallNumber then smallNumber := length+5 * width - 12 / (Factor - 3.55E13)";
            RuleParser parser = new RuleParser(RulePart.Rule);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Rule(Id(myrule)If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber))))))Then()Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook()))))))", "");
       }

        [Fact]
        public void TestOneRuleMultipleLines()
        {
            /* With carriage return/linefeed:
             * Rule myrule 
             * if length + 5 * width - 12 / (Factor - 3.55) > smallNumber 
             * then smallNumber := length + 5 * width - 12 / (Factor - 3.55E13)
             */
            string input = 
                "Rule myrule " + Environment.NewLine +
                "priority 12" + Environment.NewLine +
                "if length+5 * width - 12 / (Factor - 3.55) > smallNumber " + Environment.NewLine +
                "then smallNumber := length+5 * width - 12 / (Factor - 3.55E13)";

            RuleParser parser = new RuleParser(RulePart.Rule);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Rule(Id(myrule)Prio(Integer(12))If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber))))))Then()Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook()))))))", "");
        }

        [Fact]
        public void TestIfIfSyntaxError()
        {
            string input = "rule myrule description if";
            RuleParser parser = new RuleParser(RulePart.Rule);
            parser.Parse(input);

            Assert.True(parser.SyntaxErrors.Count == 5 &&
              parser.SyntaxErrors[0].Item1 ==
                 "Syntax error at Row 0 Column 24: expected String but instead encountered \"if\"") ;
        }

        [Fact]
        public void TestOneComplexRule()
        {
            string input =
                "RULE myComplexRule " + Environment.NewLine +
                "IF length+5 * width - 12 / (Factor - 3.55) > smallNumber " + Environment.NewLine +
                "AND IsTrueOrMaybeNot" + Environment.NewLine +
                "THEN smallNumber := length+5 * width - 12 / (Factor - 3.55E13)" + Environment.NewLine +
                "AND IsNotTrueAfterAll";

            RuleParser parser = new RuleParser(RulePart.Rule);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Rule(Id(myComplexRule)If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Factor(Id(IsTrueOrMaybeNot))))))Then()Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook()))))LogicalOp(AND)AssignExpr(Id(IsNotTrueAfterAll))))", "");
        }

        [Fact]
        public void TestMultipleRules()
        {
            string input =
                "RULE myComplexRule " + Environment.NewLine +
                "IF length+5 * width - 12 / (Factor - 3.55) > smallNumber " + Environment.NewLine +
                "AND IsTrueOrMaybeNot" + Environment.NewLine +
                "THEN smallNumber := length+5 * width - 12 / (Factor - 3.55E13)" + Environment.NewLine +
                "AND IsNotTrueAfterAll" + Environment.NewLine
                + Environment.NewLine +
                "RULE my2ndRule " + Environment.NewLine +
                "IF length+5 - 3.55 > smallNumber " + Environment.NewLine +
                "AND IsTrueOrMaybeNot" + Environment.NewLine +
                "THEN IsNotTrueAfterAll" + Environment.NewLine + Environment.NewLine;

            RuleParser parser = new RuleParser(RulePart.Rule);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Rule(Id(myComplexRule)If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55))))Rhook())))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Factor(Id(IsTrueOrMaybeNot))))))Then()Concl(AssignExpr(Id(smallNumber)Assign(:=)Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5))MathOp(*)Factor(Id(width)))MathOp(-)Term(Factor(Integer(12))MathOp(/)Factor(Lhook()Expr(Term(Factor(Id(Factor)))MathOp(-)Term(Factor(Float(3.55E13))))Rhook()))))LogicalOp(AND)AssignExpr(Id(IsNotTrueAfterAll))))Rule(Id(my2ndRule)If()Cond(CompExpr(Expr(Term(Factor(Id(length)))MathOp(+)Term(Factor(Integer(5)))MathOp(-)Term(Factor(Float(3.55))))Compare(>)Expr(Term(Factor(Id(smallNumber)))))LogicalOp(AND)CompExpr(Expr(Term(Factor(Id(IsTrueOrMaybeNot))))))Then()Concl(AssignExpr(Id(IsNotTrueAfterAll))))", "");
        }

        [Fact]
        public void TestRuleWithFullyQuantifiedName()
        {
            string input = "Rule Model.Persons.Occupations.Check If age >= 18 And status = legal Then isEmployable";
            RuleParser parser = new RuleParser(RulePart.Rule);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Rule(Id(Model.Persons.Occupations.Check)If()Cond(CompExpr(Expr(Term(Factor(Id(age))))Compare(>=)Expr(Term(Factor(Integer(18)))))LogicalOp(And)CompExpr(Expr(Term(Factor(Id(status))))Compare(=)Expr(Term(Factor(Id(legal))))))Then()Concl(AssignExpr(Id(isEmployable))))", "");
        }

        [Fact]
        public void TestRuleWithFullyQuantifiedIds()
        {
            string input = "Rule Model.Persons.Occupations.Check If person.age >= 18 And person.legal.status = legal Then isEmployable";
            RuleParser parser = new RuleParser(RulePart.Rule);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "");
            Assert.True(parser.ParseTree == "Rule(Id(Model.Persons.Occupations.Check)If()Cond(CompExpr(Expr(Term(Factor(Id(person.age))))Compare(>=)Expr(Term(Factor(Integer(18)))))LogicalOp(And)CompExpr(Expr(Term(Factor(Id(person.legal.status))))Compare(=)Expr(Term(Factor(Id(legal))))))Then()Concl(AssignExpr(Id(isEmployable))))", "");
        }
    }
}
