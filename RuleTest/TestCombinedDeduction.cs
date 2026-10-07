using System;
using System.Collections.Generic;
using System.Linq;
using Rules.Expression;
using Rules.Model;
using Rules.QueryInterpreter;
using Rules.Rules;
using Rules.Tasks;
using Xunit;

namespace RuleTest
{
    public class TestCombinedDeduction
    {
        private static (TTask.TaskEvaluateResults Result, RuleContainers Containers) Run(
            string source, ValueBag context, string query = "Goal")
        {
            var containers = TestHelper.ParseRules(source);
            containers.Containers.ForEach(container => container.EvaluationType = EvaluationType.Deduction);
            TestHelper.QueryToExpression(query, new Interpreter(), out var expression, out var errors);
            Assert.Empty(errors);
            return (new TTask { RuleContainers = containers }.Evaluate(context, expression), containers);
        }

        private static IEnumerable<ProofNode> Nodes(ProofNode node)
        {
            yield return node;
            foreach (var child in node.Children)
                foreach (var descendant in Nodes(child)) yield return descendant;
        }

        [Fact]
        public void AGoalInALaterContainerIsDerivedWithoutAskingForTheGoal()
        {
            var context = new ValueBag();
            var asked = new List<string>();
            context.MessageSink.GetUserInput = (id, _) =>
            {
                asked.Add(id.Name);
                return new TConstantNumValue(true);
            };
            const string source = "RuleContainer First Rule unrelated If Extra Then Other " +
                "RuleContainer Second Rule goal If Input Then Goal";

            var (result, containers) = Run(source, context);

            Assert.True(result.QueryResult);
            Assert.Equal(new[] { "Input" }, asked);
            Assert.Equal(2, containers.Containers.Count);
            Assert.Equal(new[] { 1, 1 }, containers.Containers.Select(container => container.Rules.Count));
            Assert.Single(result.Evaluations);
            Assert.Single(result.TaskTrace);
            Assert.Equal("Second", Assert.Single(result.TaskTrace[0].RuleResult.RuleTrace).ContainerName);
            Assert.Equal("All containers", result.Evaluations[0].RuleResult.Proof.Container);
        }

        [Fact]
        public void PrerequisitesCanBeDerivedAcrossContainersAndDuplicateNamesRemainDistinct()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(true));
            const string source = "RuleContainer First Rule decide If Intermediate Then Goal " +
                "RuleContainer Second Rule decide If Input Then Intermediate";

            var (result, _) = Run(source, context);

            Assert.True(result.QueryResult);
            var traces = result.TaskTrace[0].RuleResult.RuleTrace;
            Assert.Equal(2, traces.Count);
            Assert.Equal(new[] { "Second", "First" }, traces.Select(trace => trace.ContainerName));
            Assert.NotSame(traces[0].Rule, traces[1].Rule);
            var proof = result.Evaluations[0].RuleResult.Proof;
            Assert.Equal(new[] { "First/decide", "Second/decide" },
                Nodes(proof.Root).Where(node => node.Kind == ProofNodeKind.Rule).Select(node => node.Text));
        }

        [Fact]
        public void CompetingConclusionsFollowContainerThenRuleOrder()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(true));
            const string source = "RuleContainer First Rule first If Input Then Answer := \"first\" " +
                "RuleContainer Second Rule second If Input Then Answer := \"second\" " +
                "RuleContainer Third Rule goal If Answer = \"first\" Then Goal";

            var (result, _) = Run(source, context);

            Assert.True(result.QueryResult);
            Assert.Equal("first", ((TConstantStringValue)context.GetValue("Answer")).Value);
            Assert.Equal(new[] { "First", "Third" },
                result.TaskTrace[0].RuleResult.RuleTrace.Select(trace => trace.ContainerName));
        }

        [Fact]
        public void ASingleContainerKeepsItsOwnProofIdentity()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(true));
            var (result, _) = Run("RuleContainer Only Rule goal If Input Then Goal", context);
            Assert.True(result.QueryResult);
            Assert.Equal("Only", Assert.Single(result.Evaluations).RuleResult.Proof.Container);
        }
    }
}
