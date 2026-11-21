using System;
using System.Collections.Generic;
using System.Text;
using Rules.Expression;
using Rules.Model;

namespace Rules.Rules
{
    /// <summary>
    /// Contains one or more <see cref="Rule"/>s.
    /// </summary>
    public class RuleContainer
    {
        public readonly List<Rule> Rules = [];

        public string Name { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// To be executed after succesfull evaluation, to see if the overall evaluation process should stop
        /// (thus not moving on to adjacent RuleContainers).
        /// If so then the property StopRequested will be true.
        /// </summary>
        public TCompareExpression StopConditionExpr { get; set; } // TODO: pass on to engine.eval()

        public RuleEvaluateResult Evaluate(ValueBag context, EvaluationType evaluationType, TCompareExpression query = null)
        {
            return evaluationType switch
            {
                EvaluationType.FireOnce => new RuleEngine(this).EvaluateOnce(context),
                EvaluationType.Iteration => new RuleEngine(this).EvaluateByIteration(context),
                EvaluationType.Deduction => new RuleEngine(this).EvaluateByDeduction(query, context),
                _ => throw new ArgumentOutOfRangeException(nameof(evaluationType)),
            };
        }

        public override string ToString()
        {
            Rules.Sort();

            StringBuilder result = new StringBuilder();

            result.Append($"RuleContainer\t{Name}");
            result.Append(Environment.NewLine);

            for (int i = 0; i < Rules.Count; i++)
            {
                Rule rule = Rules[i];
                result.Append(rule.ToString());

                if (i < Rules.Count - 1)
                {
                    result.Append(Environment.NewLine);
                    result.Append(Environment.NewLine);
                }
            }
            return result.ToString();
        }
    }

    /// <summary>
    /// Contain a list of <see cref="RuleContainer"/>
    /// </summary>
    public class RuleContainers
    {
        public readonly List<RuleContainer> Containers = new List<RuleContainer>();

        public void AddRuleContainer(RuleContainer ruleContainer)
        {
            Containers.Add(ruleContainer);
        }

        public void Clear()
        {
            Containers.Clear();
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < Containers.Count; i++)
            {
                RuleContainer ruleContainer = Containers[i];
                result.Append(ruleContainer.ToString());

                if (i < Containers.Count - 1)
                {
                    result.Append(Environment.NewLine);
                    result.Append(Environment.NewLine);
                }
            }
            return result.ToString();
        }
    }
}
