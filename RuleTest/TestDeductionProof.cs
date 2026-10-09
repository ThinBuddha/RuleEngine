using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rules.Expression;
using Rules.Model;
using Rules.QueryInterpreter;
using Rules.Rules;
using Xunit;

namespace RuleTest
{
    public class TestDeductionProof
    {
        private static RuleEvaluateResult Run(string source, ValueBag context, string query = "Goal")
        {
            var container = TestHelper.ParseRules(source).Containers[0];
            TestHelper.QueryToExpression(query, new Interpreter(), out var expression, out var errors);
            Assert.Empty(errors);
            return container.Evaluate(context, EvaluationType.Deduction, expression);
        }

        private static IEnumerable<ProofNode> Nodes(ProofNode node)
        {
            yield return node;
            foreach (var child in node.Children)
                foreach (var descendant in Nodes(child)) yield return descendant;
        }

        [Fact]
        public void NestedProofAndWhySnapshotFollowTheActiveBranch()
        {
            var context = new ValueBag();
            DeductionProof why = null;
            context.MessageSink.GetUserInput = (subject, bag) =>
            {
                Assert.Equal("Input", subject.Name);
                why = bag.GetDeductionProof();
                var active = Nodes(why.Root).Single(n => n.Id == why.ActiveNodeId);
                Assert.Equal(ProofState.WaitingForInput, active.State);
                Assert.Equal(EvidenceSource.UserAnswer, active.Source);
                Assert.Equal(new[] { "Demo/outer", "Demo/inner" },
                    Nodes(why.Root).Where(n => n.Kind == ProofNodeKind.Rule).Select(n => n.Text));
                return new TConstantNumValue(12);
            };
            var result = Run("RuleContainer Demo Rule outer If Intermediate Then Goal Rule inner If Input > 10 Then Intermediate", context);
            Assert.True(result.ProvenByInduction);
            Assert.Equal(ProofState.Succeeded, result.Proof.Root.State);
            Assert.Null(result.Proof.ActiveNodeId);
            Assert.Null(context.GetDeductionProof());
            Assert.Equal(2, result.RuleTrace.Count);
            var nodes = Nodes(result.Proof.Root).ToList();
            Assert.Equal(nodes.Count, nodes.Select(n => n.Id).Distinct().Count());
            Assert.Contains(nodes, n => n.Source == EvidenceSource.UserAnswer && Equals(n.Value, 12));
            Assert.Contains(nodes, n => n.Kind == ProofNodeKind.Operand && Equals(n.Value, 10));
            // A stored Why snapshot must not change when evaluation resumes.
            Assert.Equal(ProofState.WaitingForInput, Nodes(why.Root).Single(n => n.Id == why.ActiveNodeId).State);
            Assert.DoesNotContain(Nodes(why.Root), n => n.Kind == ProofNodeKind.Conclusion);
            Assert.Contains(Nodes(nodes.Single(n => n.Text == "Demo/outer")), n => n.Text == "Demo/inner");
        }

        [Fact]
        public void FailedAlternativesAndSkippedConditionsAreRetained()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(2));
            context.MessageSink.GetUserInput = (_, _) => throw new Exception("Skipped condition must not prompt");
            var result = Run("RuleContainer Demo Rule first If Input = 1 and Missing Then Goal Rule second If Input = 2 Then Goal", context);
            var rules = Nodes(result.Proof.Root).Where(n => n.Kind == ProofNodeKind.Rule).ToList();
            Assert.Equal(ProofState.Failed, rules[0].State);
            Assert.Equal(ProofState.Skipped, rules[0].Children[1].State);
            Assert.Equal(ProofState.Succeeded, rules[1].State);
            Assert.Single(result.RuleTrace);
        }

        [Fact]
        public void ReusedCalculatedFactsReferenceTheirOriginalConclusion()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Base", new TConstantNumValue(4));
            var result = Run("RuleContainer Demo Rule outer If Total = 8 and Total > 5 Then Goal Rule calculate If Base > 0 Then Total := Base * 2", context);
            var nodes = Nodes(result.Proof.Root).ToList();
            var conclusion = Assert.Single(nodes, n => n.Kind == ProofNodeKind.Conclusion && n.Text.StartsWith("Total"));
            Assert.Equal(8, conclusion.Value);
            Assert.Equal(EvidenceSource.RuleConclusion, conclusion.Source);
            Assert.Contains(Nodes(conclusion), n => n.Source == EvidenceSource.SuppliedFact && n.Text == "Base" && Equals(n.Value, 4));
            Assert.Equal(2, nodes.Count(n => n.Kind == ProofNodeKind.Evidence && n.EvidenceId == conclusion.Id));
            Assert.Single(nodes, n => n.Kind == ProofNodeKind.Rule && n.Text == "Demo/calculate");
            context.AddOrUpdateValue("Total", new TConstantNumValue(99));
            Assert.Equal(8, conclusion.Value);
            Assert.All(nodes.Where(n => n.EvidenceId.HasValue), n => Assert.Contains(nodes, target => target.Id == n.EvidenceId));
        }

        [Fact]
        public void DeclinedInputIsUnknownNotFalse()
        {
            var context = new ValueBag();
            context.MessageSink.GetUserInput = (_, _) => null;
            var result = Run("RuleContainer Demo Rule test If Input Then Goal", context);
            Assert.Equal(ProofState.Unknown, result.Proof.Root.State);
            var input = Assert.Single(Nodes(result.Proof.Root), n => n.Source == EvidenceSource.UserAnswer);
            Assert.Equal(ProofState.Unknown, input.State);
            Assert.Null(input.Value);
            Assert.Null(context.GetDeductionProof());
        }

        [Fact]
        public void FalseFactIsResolvedEvidenceAndNegationIsRecordedSeparately()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(false));
            var result = Run("RuleContainer Demo Rule test If not Input Then Goal", context);
            Assert.True(result.ProvenByInduction);
            var evidence = Assert.Single(Nodes(result.Proof.Root), n => n.Source == EvidenceSource.SuppliedFact);
            Assert.Equal(false, evidence.Value);
            Assert.Equal(ProofState.Succeeded, evidence.State);
            Assert.Contains(Nodes(result.Proof.Root), n => n.Kind == ProofNodeKind.Condition && Equals(n.Value, true));
        }

        [Fact]
        public void FalseQueryAndUnestablishedQueryHaveDifferentProofStates()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Goal", new TConstantNumValue(false));
            var result = Run("RuleContainer Demo Rule test If Input Then Goal", context);
            Assert.False(result.ProvenByInduction);
            Assert.Equal(ProofState.Failed, result.Proof.Root.State);
            Assert.DoesNotContain(Nodes(result.Proof.Root), n => n.Kind == ProofNodeKind.Rule);
        }

        [Fact]
        public void CancelledCallbackRestoresContextAndKeepsSnapshotStable()
        {
            var context = new ValueBag();
            DeductionProof why = null;
            context.MessageSink.GetUserInput = (_, bag) =>
            {
                why = bag.GetDeductionProof();
                throw new OperationCanceledException();
            };
            Assert.Throws<OperationCanceledException>(() => Run("RuleContainer Demo Rule test If Input Then Goal", context));
            Assert.Null(context.GetDeductionProof());
            Assert.Null(context.MessageSink.NotifyUnknownValue);
            Assert.Equal(ProofState.WaitingForInput, Nodes(why.Root).Single(n => n.Id == why.ActiveNodeId).State);
            context.MessageSink.GetUserInput = (_, _) => new TConstantNumValue(true);
            Assert.True(Run("RuleContainer Demo Rule test If Input Then Goal", context).ProvenByInduction);
        }

        [Fact]
        public async Task ParallelPromptsHaveIndependentProofs()
        {
            using var barrier = new Barrier(2);
            Task<DeductionProof> Session(int value) => Task.Run(() =>
            {
                var context = new ValueBag();
                context.MessageSink.GetUserInput = (_, bag) =>
                {
                    Assert.True(barrier.SignalAndWait(TimeSpan.FromSeconds(10)));
                    Assert.Equal(ProofState.WaitingForInput,
                        Nodes(bag.GetDeductionProof().Root).Single(n => n.Id == bag.GetDeductionProof().ActiveNodeId).State);
                    return new TConstantNumValue(value);
                };
                return Run($"RuleContainer Demo Rule test If Input = {value} Then Goal", context).Proof;
            });
            var proofs = await Task.WhenAll(Session(11), Session(22));
            Assert.Equal(11, Nodes(proofs[0].Root).Single(n => n.Source == EvidenceSource.UserAnswer).Value);
            Assert.Equal(22, Nodes(proofs[1].Root).Single(n => n.Source == EvidenceSource.UserAnswer).Value);
        }

        [Theory]
        [InlineData(EvaluationType.FireOnce)]
        [InlineData(EvaluationType.Iteration)]
        public void ForwardModesDoNotCreateDeductionProofs(EvaluationType mode)
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Input", new TConstantNumValue(true));
            var container = TestHelper.ParseRules("RuleContainer Demo Rule test If Input Then Goal").Containers[0];
            Assert.Null(container.Evaluate(context, mode).Proof);
            Assert.Null(context.GetDeductionProof());
        }

        [Fact]
        public void NestedEvaluationRestoresTheOuterWhyTree()
        {
            var context = new ValueBag();
            context.AddOrUpdateValue("Known", new TConstantNumValue(true));
            context.MessageSink.GetUserInput = (_, bag) =>
            {
                var before = bag.GetDeductionProof();
                var nested = Run("RuleContainer Nested Rule inner If Known Then InnerGoal", bag, "InnerGoal");
                Assert.True(nested.ProvenByInduction);
                Assert.Equal(before.ActiveNodeId, bag.GetDeductionProof().ActiveNodeId);
                Assert.Equal(before.Root.Text, bag.GetDeductionProof().Root.Text);
                return new TConstantNumValue(true);
            };
            var result = Run("RuleContainer Demo Rule outer If Input Then Goal", context);
            Assert.True(result.ProvenByInduction);
            Assert.DoesNotContain(Nodes(result.Proof.Root), n => n.Text == "Nested/inner");
            Assert.Null(context.GetDeductionProof());
        }

        [Fact]
        public void LaterRunsTreatRetainedFactsAsSuppliedAndDoNotReuseProofs()
        {
            var context = new ValueBag();
            context.MessageSink.GetUserInput = (_, _) => new TConstantNumValue(true);
            const string source = "RuleContainer Demo Rule test If Input Then Goal";
            var first = Run(source, context);
            var second = Run(source, context);
            Assert.DoesNotContain(Nodes(second.Proof.Root), n => n.Kind == ProofNodeKind.Rule);
            Assert.Contains(Nodes(second.Proof.Root), n => n.Text == "Goal" && n.Source == EvidenceSource.SuppliedFact);
            context.Clear();
            var third = Run(source, context);
            Assert.Contains(Nodes(third.Proof.Root), n => n.Source == EvidenceSource.UserAnswer);
            Assert.NotSame(first.Proof.Root, third.Proof.Root);
        }

        [Fact]
        public void OverwrittenFactsRetainTheEvidenceValuesUsedByEarlierConditions()
        {
            var context = new ValueBag();
            var original = new TConstantNumValue(1);
            context.AddOrUpdateValue("Input", original);
            var result = Run("RuleContainer Demo Rule test If Input = 1 Then Input := 2 and Goal", context);
            var nodes = Nodes(result.Proof.Root).ToList();
            original.Value.IntValue = 99;
            Assert.Equal(1, Assert.Single(nodes, n => n.Source == EvidenceSource.SuppliedFact).Value);
            Assert.Equal(2, Assert.Single(nodes, n => n.Kind == ProofNodeKind.Conclusion && n.Text.StartsWith("Input")).Value);
        }
    }
}
