using System;
using Rules.Expression;
using Rules.Model;
using Rules.Rules;
using Xunit;

namespace RuleTest
{
    public class TestRuleIteration
    {
        [Fact]
        public void FiredRuleDoesNotReapplyItsConclusions()
        {
            var containers = TestHelper.ParseRules("RuleContainer Test Rule increment If Count < 3 Then Count := Count + 1");
            var context = new ValueBag();
            context.AddOrUpdateValue("Count", new TConstantNumValue(0));

            var result = new RuleEngine(containers.Containers[0]).EvaluateByIteration(context);

            Assert.Equal(1, ((TConstantNumValue)context.GetValue("Count")).Value.IntValue);
            Assert.Single(result.RuleTrace);

            // Tracking belongs to this invocation, not to the shared rule objects.
            new RuleEngine(containers.Containers[0]).EvaluateByIteration(context);
            Assert.Equal(2, ((TConstantNumValue)context.GetValue("Count")).Value.IntValue);
        }

        [Fact]
        public void UnfiredRuleIsRetriedAfterAnotherRuleEnablesIt()
        {
            var containers = TestHelper.ParseRules("RuleContainer Test Rule finish If Stage = 1 Then Stage := 2 Rule start If Stage = 0 Then Stage := 1");
            var context = new ValueBag();
            context.AddOrUpdateValue("Stage", new TConstantNumValue(0));

            var result = new RuleEngine(containers.Containers[0]).EvaluateByIteration(context);

            Assert.Equal(2, ((TConstantNumValue)context.GetValue("Stage")).Value.IntValue);
            Assert.Equal(2, result.RuleTrace.Count);
        }

        [Fact]
        public void AlwaysTrueRuleIsNotEvenEvaluatedAgain()
        {
            var containers = TestHelper.ParseRules("RuleContainer Test Rule stable If Flag Then Done");
            var condition = new SingleEvaluationCondition();
            containers.Containers[0].Rules[0].Conditions[0].LeftExpr.Terms[0].Factors[0] = condition;

            var result = new RuleEngine(containers.Containers[0]).EvaluateByIteration(new ValueBag());

            Assert.Single(result.RuleTrace);
            Assert.Equal(1, condition.Calls);
        }

        private sealed class SingleEvaluationCondition : TFactor
        {
            public int Calls { get; private set; }
            public override TConstantValue Evaluate(ValueBag context)
            {
                // Fail promptly instead of hanging the test if iteration regresses.
                if (++Calls > 1) throw new InvalidOperationException("An already-fired rule was evaluated again.");
                return new TConstantNumValue(true);
            }
        }
    }
}
