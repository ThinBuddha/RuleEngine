using Rules;
using Rules.Expression;
using Rules.Model;
using Rules.Parser;
using Rules.QueryInterpreter;
using Rules.Rules;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuleTest
{
    public static class TestHelper
    {
        public static RuleContainers ParseRules(string input)
        {
            RuleParser parser = new RuleParser(RulePart.RuleContainer);
            parser.Parse(input);
            Assert.True(parser.SyntaxErrors.Count == 0, "Wrong test input, it contains syntax errors");
            var ruleContainers = (parser.Builder as RulesBuilder).RuleContainers;
            Assert.NotNull(ruleContainers);
            return ruleContainers;
        }

        public static void QueryToExpression(string query, Interpreter interpreter, out TCompareExpression compareExpr, out string errors)
        {
            List<Tuple<string, string>> errorList;
            errors = string.Empty;

            (compareExpr, errorList) = Interpreter.QueryToExpression(query);
            if (errorList != null)
            {
                foreach (Tuple<string, string> e in errorList)
                {
                    errors += $"{e.Item1},{e.Item2}" + Environment.NewLine;
                }
            }
        }

        public static RuleContainers GetTravelRules()
        {
            StringBuilder input = new StringBuilder()
              .AppendLine("RuleContainer Determine.how.to.travel")
              .AppendLine("Rule walking.1    If not Outside.it.rains and Distance.to.travel = \"small\" then Travel.by.walking")
              .AppendLine("Rule walking.2    If not Outside.it.rains and Distance.to.travel = \"medium\" and Stamina.of.person = \"great\" then Travel.by.walking")
              .AppendLine("Rule walking.3    If Outside.it.rains and Distance.to.travel = \"small\" and Person.has.umbrella then Travel.by.walking")
              .AppendLine("Rule walking.4    If Outside.it.rains and Distance.to.travel = \"medium\" and Person.has.umbrella and Stamina.of.person = \"great\" then Travel.by.walking")
              .AppendLine("Rule car.1        If Outside.it.rains and Distance.to.travel = \"big\" and Person.has.car and Price.of.gas = \"good\" then Travel.by.car")
              .AppendLine("Rule train.1      If Outside.it.rains and Distance.to.travel = \"big\" and Person.has.car and Price.of.gas <> \"good\" then Travel.by.train")
              .AppendLine("Rule train.2      If Outside.it.rains and Distance.to.travel = \"big\" and not Person.has.car then Travel.by.train")
              .AppendLine("Rule gas.price.1  If Travel.country = \"Netherlands\" then Price.of.gas := \"bad\"")
              .AppendLine("Rule gas.price.2  If Travel.country <> \"Netherlands\" then Price.of.gas := \"good\"");
            RuleContainers ruleContainers = ParseRules(input.ToString());
            Assert.Single(ruleContainers.Containers);
            return ruleContainers;
        }
    }

    /// <summary>
    /// Mock user answers
    /// </summary>
    public class UserAnswer
    {
        private readonly Dictionary<string, TConstantValue> _table = new Dictionary<string, TConstantValue>();

        public void Initialize(ValueBag context)
        {
            _table.Clear();
            context.MessageSink.GetUserInput = (subject, _) => _table.TryGetValue(subject.Name, out var value) ? value.Clone() : null;
            context.MessageSink.NotifyUnknownValue = context.MessageSink.GetUserInput;
        }
        public void AddAnswer(string Id, string value) => _table.Add(Id, new TConstantStringValue(value));
        public void AddAnswer(string Id, int value) => _table.Add(Id, new TConstantNumValue(value));
        public void AddAnswer(string Id, double value) => _table.Add(Id, new TConstantNumValue(value));
        public void AddAnswer(string Id, bool value) => _table.Add(Id, new TConstantNumValue(value));
    }
}
