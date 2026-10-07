using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Rules.Expression;
using Rules.Model;
using Percentage = Probabilities.Probability;

namespace Rules.Rules
{
    /// <summary>
    /// A list of conditions and optionally a list of conclusions.
    /// </summary>
    public class Rule : IComparable<Rule>
    {
        public string Name { get; private set; }
        public int Priority { get; set; } = 1;
        public Percentage Weight { get; set; } = Percentage.Max; // certainty or weight, however you look at it
        public bool Stop { get; set; } = false;
        public List<TCompareExpression> Conditions { get; } = new List<TCompareExpression>();
        public List<TAssignExpression> Conclusions { get; } = new List<TAssignExpression>();

        int IComparable<Rule>.CompareTo(Rule other)
        {
            if (other == null)
            {
                return 1;
            }
            if (Priority < other.Priority)
            {
                return -1;
            }
            if (Priority > other.Priority)
            {
                return 1;
            }
            return 0;
        }

        public bool Evaluate(ValueBag context)
        {
            foreach (TCompareExpression condition in Conditions)
            {
                if (!condition.Evaluate(context))
                {
                    return false;
                }             
            }
            AssertConclusions(context);
            return true;
        }

        public void AssertConclusions(ValueBag context)
        {
            if (Conclusions != null)
            {
                foreach (TAssignExpression conclusion in Conclusions)
                {
                    conclusion.Evaluate(context);
                }
            }
        }

        public void AddCondition(TCompareExpression compareExpr)
        {
            Conditions.Add(compareExpr);
        }

        // The only allowed operator in a conclusion is ":=" ("assign")
        // some concepts might/need to be added, like "range":
        // e.g. ... then Employee1.Salary.Range := [60K, 100K] ("lies between this and that")
        public void AddConclusion(TAssignExpression assignExpr)
        {
            Conclusions.Add(assignExpr);
        }

        public void SetName(string value)
        {
            Name = value;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append($"Rule\t\t{Name}");
            result.Append(Environment.NewLine);
            result.Append($"Priority\t{Priority}");
            result.Append(Environment.NewLine);
            result.Append($"Weight\t\t{Weight}");
            result.Append(Environment.NewLine);

            result.Append("If\t\t");
            for (int i = 0; i < Conditions.Count; i++)
            {
                TCompareExpression expr = Conditions[i];
                result.Append(expr.ToString());

                if (i < Conditions.Count - 1)
                {
                    result.Append(Environment.NewLine);
                    result.Append("And\t\t");
                }
            }
            result.Append(Environment.NewLine);
            result.Append("Then\t\t");
            for (int i = 0; i < Conclusions.Count; i++)
            {
                TAssignExpression expr = Conclusions[i];
                result.Append(expr.ToString());

                if (i < Conclusions.Count - 1)
                {
                    result.Append(Environment.NewLine);
                    result.Append("And\t\t\t");
                }
            }

            return result.ToString();
        }

        public override bool Equals(object rule)
        {
            return this == rule || Name == (rule as Rule).Name; 
        }
    }

    /// <summary>
    /// Depending on the type of rule(s) and evaluation method, can be a list of rules or a rule-tree
    /// </summary>
    public class RuleTrace
    {
        public readonly Rule Rule;
        public readonly string ContainerName;

        public RuleTrace(Rule rule, string containerName = null)
        {
            Rule = rule;
            ContainerName = containerName;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"\tRule \"{Rule.Name}\"{(ContainerName == null ? "" : $" in container \"{ContainerName}\"")} says:");
            builder.Append(Environment.NewLine);
            foreach (TAssignExpression conclusion in Rule.Conclusions)
            {
                builder.Append($"\t\t{conclusion}");
                builder.Append(Environment.NewLine);
            }

            builder.Append("\tBecause:");
            builder.Append(Environment.NewLine);
            foreach (TCompareExpression condition in Rule.Conditions)
            {
                builder.Append($"\t\t{condition}");
                builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }
    }

    public class RuleEvaluateResult
    {
        public DeductionProof Proof { get; internal set; }
        public readonly List<RuleTrace> RuleTrace = new List<RuleTrace>();
        public bool StopConditionValidated { get; set; }
        /// <summary>
        /// If <see cref="RuleGotFired"/> it true then at least one rule in the <see cref="RuleContainer"/> got fired.
        /// </summary>
        public bool RuleGotFired { get { return RuleTrace.Count > 0; } }

        /// <summary>
        /// In case of deduction, if a query result could not be determined the property ProvenByInduction will be null. Otherwise true (proven to be true) or false (proven to be false)
        /// In case of other types of inteference the property will by definition be null (not applicable)
        /// </summary>
        public bool? ProvenByInduction { get; internal set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"\tStop condition validated: {StopConditionValidated}");
            builder.Append(Environment.NewLine);
            builder.Append(Environment.NewLine);
            foreach (RuleTrace ruleTrace in RuleTrace)
            {
                builder.Append($"{ruleTrace}");
                builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }

        public RuleTrace AddOrGetRule(Rule rule, string containerName = null)
        {
            RuleTrace ruleTrace = RuleTrace.Find(t => ReferenceEquals(t.Rule, rule));
            if (ruleTrace == null)
            {
                ruleTrace = new RuleTrace(rule, containerName);
                RuleTrace.Add(ruleTrace);
            }
            return ruleTrace;
        }

        public void DeleteRule(Rule rule)
        {
            for (int i = RuleTrace.Count - 1; i >= 0; i--)
            {
                if (RuleTrace[i].Rule == rule)
                {
                    RuleTrace.RemoveAt(i);
                    break;
                }
            }
        }
    }

}
