using System;
using Rules;
using Rules.Expression;
using Rules.Model;
using Rules.Rules;
using Xunit;

namespace RuleTest
{
    public class TestSessionIsolation
    {
        [Fact]
        public void ContextsOnTheSameThreadHaveIndependentInputCallbacks()
        {
            var first = new ValueBag();
            var second = new ValueBag();
            first.MessageSink.NotifyUnknownValue = (_, _) => new TConstantNumValue(11);
            second.MessageSink.NotifyUnknownValue = (_, _) => new TConstantNumValue(22);

            Assert.NotSame(first.MessageSink, second.MessageSink);
            Assert.Equal(11, ((TConstantNumValue)new TId { Name = "Input" }.Evaluate(first)).Value.IntValue);
            Assert.Equal(22, ((TConstantNumValue)new TId { Name = "Input" }.Evaluate(second)).Value.IntValue);
            first.Clear();
            Assert.Equal(22, ((TConstantNumValue)second.GetValue("Input")).Value.IntValue);
        }

        [Fact]
        public void NestedSessionsDoNotReplaceTheOuterEnginesResolver()
        {
            var first = new ValueBag();
            var second = new ValueBag();
            var outer = TestHelper.ParseRules("RuleContainer Outer Rule check If Input = 11 and Other = 11 Then Done").Containers[0];
            var inner = TestHelper.ParseRules("RuleContainer Inner Rule check If Input = 22 Then Done").Containers[0];
            var outerEngine = new RuleEngine(outer);
            var innerEngine = new RuleEngine(inner);
            second.MessageSink.GetUserInput = (_, _) => new TConstantNumValue(22);
            int calls = 0;
            first.MessageSink.GetUserInput = (_, _) =>
            {
                if (++calls == 1) innerEngine.EvaluateOnce(second);
                return new TConstantNumValue(11);
            };

            var result = outerEngine.EvaluateOnce(first);

            Assert.Single(result.RuleTrace);
            Assert.Equal(2, calls);
            Assert.Equal(11, ((TConstantNumValue)first.GetValue("Input")).Value.IntValue);
            Assert.Equal(22, ((TConstantNumValue)second.GetValue("Input")).Value.IntValue);
            Assert.Null(first.MessageSink.NotifyUnknownValue);
            Assert.Null(second.MessageSink.NotifyUnknownValue);
        }

        [Fact]
        public void FailedEvaluationRestoresTheContextsOriginalResolver()
        {
            var context = new ValueBag();
            NotifyUnknownValueEvent original = (_, _) => new TConstantNumValue(3);
            context.MessageSink.NotifyUnknownValue = original;
            context.MessageSink.GetUserInput = (_, _) => throw new InvalidOperationException("Cancelled session");
            var container = TestHelper.ParseRules("RuleContainer Test Rule check If Input = 1 Then Done").Containers[0];

            Assert.Throws<InvalidOperationException>(() => new RuleEngine(container).EvaluateOnce(context));
            Assert.Same(original, context.MessageSink.NotifyUnknownValue);
            Assert.Empty(context.Values);
        }
    }
}
