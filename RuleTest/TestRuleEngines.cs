using Rules;
using Rules.Expression;
using Rules.Model;
using Rules.Parser;
using Rules.QueryInterpreter;
using Rules.Rules;
using Rules.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuleTest
{
    public class TestRuleEngines
    {
        private static readonly Interpreter _interpreter = new Interpreter();
        private static readonly ValueBag _context = new ValueBag();

       // BECOMES OBSOLETE

        [Fact]
        public void TestDeductionMultipleRules_Chaining_True()
        {
            // Mock user answers
            UserAnswer.Initialize();
            UserAnswer.AddAnswer("Outside.it.rains", true);
            UserAnswer.AddAnswer("Stamina.of.person", "lousy");
            UserAnswer.AddAnswer("Distance.to.travel", "big");
            UserAnswer.AddAnswer("Person.has.car", true);
            UserAnswer.AddAnswer("Travel.country", "Bulgaria");

            // Setup the rules
            RuleContainers ruleContainers = TestHelper.GetTravelRules();

            // Setup the query
            string query = "Travel.by.car";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rules
            ruleContainers.Containers[0].EvaluationType = EvaluationType.Deduction;
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, compareExpr);
            Assert.NotNull(evalResult);
            Assert.NotNull(evalResult.QueryResult);
            Assert.True(evalResult.QueryResult);

            // TODO: inspect Traces

            // TODO: also test with Travel.country = "Netherlands" user answer, this way we test pass through to user answer instead of deriving Price.of.gas
        }

        [Fact]
        public void TestDeductionMultipleRules_Chaining_False()
        {
            // Mock user answers
            UserAnswer.Initialize();
            UserAnswer.AddAnswer("Outside.it.rains", true);
            UserAnswer.AddAnswer("Person.has.umbrella", true);
            UserAnswer.AddAnswer("Stamina.of.person", "lousy");
            UserAnswer.AddAnswer("Distance.to.travel", "big");
            UserAnswer.AddAnswer("Travel.country", "Bulgaria");

            // Setup the rules
            RuleContainers ruleContainers = TestHelper.GetTravelRules();

            // Setup the query ("Travel.by.walking" should be false)
            string query = "Travel.by.walking";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rules
            ruleContainers.Containers[0].EvaluationType = EvaluationType.Deduction;
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, compareExpr);
            Assert.NotNull(evalResult);
            Assert.NotNull(evalResult.QueryResult);
            Assert.False(evalResult.QueryResult);
        }
      
    }
}
