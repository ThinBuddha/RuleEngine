using Rules.Expression;
using Rules.Model;
using Rules.QueryInterpreter;
using Rules.Rules;
using Rules.Tasks;
using Xunit;

namespace RuleTest
{
    public class TestRuleEngines
    {
        private readonly Interpreter _interpreter = new Interpreter();
        private readonly ValueBag _context = new ValueBag();
        private readonly UserAnswer _userAnswer = new UserAnswer();

       // BECOMES OBSOLETE

        [Fact]
        public void TestDeductionMultipleRules_Chaining_True()
        {
            // Mock user answers
            _userAnswer.Initialize(_context);
            _userAnswer.AddAnswer("Outside.it.rains", true);
            _userAnswer.AddAnswer("Stamina.of.person", "lousy");
            _userAnswer.AddAnswer("Distance.to.travel", "big");
            _userAnswer.AddAnswer("Person.has.car", true);
            _userAnswer.AddAnswer("Travel.country", "Bulgaria");

            // Setup the rules
            RuleContainers ruleContainers = TestHelper.GetTravelRules();

            // Setup the query
            string query = "Travel.by.car";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rules
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
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
            _userAnswer.Initialize(_context);
            _userAnswer.AddAnswer("Outside.it.rains", true);
            _userAnswer.AddAnswer("Person.has.umbrella", true);
            _userAnswer.AddAnswer("Stamina.of.person", "lousy");
            _userAnswer.AddAnswer("Distance.to.travel", "big");
            _userAnswer.AddAnswer("Travel.country", "Bulgaria");

            // Setup the rules
            RuleContainers ruleContainers = TestHelper.GetTravelRules();

            // Setup the query ("Travel.by.walking" should be false)
            string query = "Travel.by.walking";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rules
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
            Assert.NotNull(evalResult);
            Assert.NotNull(evalResult.QueryResult);
            Assert.False(evalResult.QueryResult);
        }
      
    }
}
