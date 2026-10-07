using System;
using System.Linq;
using Rules.Expression;
using Rules.Model;
using Rules.QueryInterpreter;
using Rules.Rules;
using Rules.Tasks;
using Xunit;

namespace RuleTest
{
    public class TestTaskEvaluationMode
    {
        private static TCompareExpression Query(string text)
        {
            TestHelper.QueryToExpression(text, new Interpreter(), out var expression, out var errors);
            Assert.Empty(errors);
            return expression;
        }

        [Fact]
        public void RequestedDeductionOverridesMixedContainerModesWithoutMutatingThem()
        {
            var containers = TestHelper.ParseRules(
                "RuleContainer First Rule goal If Intermediate Then Goal " +
                "RuleContainer Second Rule derive If Input Then Intermediate");
            containers.Containers[0].EvaluationType = EvaluationType.Iteration;
            containers.Containers[1].EvaluationType = EvaluationType.FireOnce;
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(true));

            var result = new TTask { RuleContainers = containers }
                .Evaluate(context, Query("Goal"), EvaluationType.Deduction);

            Assert.True(result.QueryResult);
            Assert.Single(result.Evaluations);
            Assert.Equal("All containers", result.Evaluations[0].RuleResult.Proof.Container);
            Assert.Equal(new[] { "Iteration", "FireOnce" },
                containers.Containers.Select(container => container.EvaluationType.ToString()));
            Assert.Equal(new[] { "Second", "First" },
                result.TaskTrace[0].RuleResult.RuleTrace.Select(trace => trace.ContainerName));
        }

        [Fact]
        public void RequestedFireOnceUsesInputResolutionEvenIfContainerSaysDeduction()
        {
            var containers = TestHelper.ParseRules("RuleContainer Demo Rule goal If Input Then Goal");
            containers.Containers[0].EvaluationType = EvaluationType.Deduction;
            var context = new ValueBag();
            context.MessageSink.GetUserInput = (_, _) => new TConstantNumValue(true);

            var result = new TTask { RuleContainers = containers }
                .Evaluate(context, requestedType: EvaluationType.FireOnce);

            Assert.Single(result.TaskTrace);
            Assert.Null(result.Evaluations[0].RuleResult.Proof);
            Assert.True(((TConstantNumValue)context.GetValue("Goal")).Value.BoolValue);
            Assert.Equal(EvaluationType.Deduction, containers.Containers[0].EvaluationType);
        }

        [Fact]
        public void RequestedIterationRetriesRulesEvenIfContainerSaysFireOnce()
        {
            var containers = TestHelper.ParseRules(
                "RuleContainer Demo Rule goal If Intermediate Then Goal Rule derive If Input Then Intermediate");
            containers.Containers[0].EvaluationType = EvaluationType.FireOnce;
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(true));
            context.MessageSink.GetUserInput = (_, _) => new TConstantNumValue(false);

            var result = new TTask { RuleContainers = containers }
                .Evaluate(context, requestedType: EvaluationType.Iteration);

            Assert.Equal(2, result.TaskTrace[0].RuleResult.RuleTrace.Count);
            Assert.True(((TConstantNumValue)context.GetValue("Goal")).Value.BoolValue);
            Assert.Null(result.Evaluations[0].RuleResult.Proof);
            Assert.Equal(EvaluationType.FireOnce, containers.Containers[0].EvaluationType);
        }

        [Fact]
        public void UnspecifiedModeStillUsesContainerConfiguration()
        {
            var containers = TestHelper.ParseRules("RuleContainer Demo Rule goal If Input Then Goal");
            containers.Containers[0].EvaluationType = EvaluationType.Deduction;
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(true));

            var result = new TTask { RuleContainers = containers }.Evaluate(context, Query("Goal"));

            Assert.True(result.QueryResult);
            Assert.Equal("Demo", Assert.Single(result.Evaluations).RuleResult.Proof.Container);
        }
    }
}
