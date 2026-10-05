using Rules.Rules;
using Rules.Model;
using System.Collections.Generic;
using System.Text;
using System;
using Rules.Expression;

namespace Rules.Tasks
{
    /// <summary>
    /// Contains one or more <see cref="RuleContainer"/>s and evaluates them sequentially.
    /// An optional query will also be evaluated, after the <see cref="RuleContainer"/>s evaluation.
    /// </summary>
    /// <remarks>
    /// Each <see cref="RuleContainer"/> is evaluated. 
    /// As a "strategical measure", a <see cref="RuleContainer"/> can request to stop the rest of the process. 
    /// If the request is honored, the remaining <see cref="RuleContainer"/>s are skipped.
    /// To prevent confusion with the Threading.Task class, this class is prefixed with a 'T'.
    /// </remarks>
    public class TTask
    {
        public RuleContainers RuleContainers { get; set; }

        public TaskEvaluateResults Evaluate(ValueBag context, TCompareExpression query = null)
        {
            TaskEvaluateResults taskResults = new TaskEvaluateResults();
            RuleEvaluateResult rulesResult = null;
            foreach (RuleContainer container in RuleContainers.Containers)
            {
                rulesResult = container.Evaluate(context, query);
                if (rulesResult.RuleGotFired)
                {
                    taskResults.AddTask(container, rulesResult);

                    if (rulesResult.StopConditionValidated)
                    {
                        // We haven't got any logic to weigh the consequences on our level of perception,
                        // so we simply agree, for now...
                        // A future improvement may be to (also) delegate this request to a Strategy layer
                        break;
                    }
                }
            }
            if (query != null && rulesResult != null)
            {
                // The rules were executed to answer a query (instead of causing state changes)
                taskResults.QueryResult = rulesResult.ProvenByInduction; 
            }

            return taskResults;
        }

        public void Clear()
        {
            // This removes the reference to my RuleContainers, it does not clear the containers themselves
            RuleContainers = null;
        }

        /// <summary>
        /// Contains trace of what happened for an evaluated <see cref="RuleContainer"/>
        /// </summary>
        public class TaskTrace
        {
            public readonly RuleContainer RuleContainer;
            public readonly RuleEvaluateResult RuleResult;

            public TaskTrace(RuleContainer ruleContainer, RuleEvaluateResult ruleResult)
            {
                RuleContainer = ruleContainer;
                RuleResult = ruleResult;
            }

            public override string ToString()
            {
                return new StringBuilder()
                  .Append($"RuleContainer \"{RuleContainer.Name}\" says:")
                  .Append(Environment.NewLine)
                  .Append($"\t{RuleResult.RuleTrace.Count} rules were applied")
                  .Append(Environment.NewLine)
                  .Append($"{RuleResult}")
                  .Append($"Request to halt process was {RuleResult.StopConditionValidated}")
                  .ToString();
            }
        }

        public class TaskEvaluateResults
        {
            public readonly List<TaskTrace> TaskTrace = new();
            public bool StopConditionValidated { get; set; }
            /// <summary>
            /// True if a query got proven to be true, false if a query got proven to be false and null if it remains
            /// inconclusive due to missing data and/or missing rules.
            /// </summary>
            public bool? QueryResult { get; set; } 

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append($"There were {TaskTrace.Count} tasks executed.");
                builder.Append(Environment.NewLine);
                builder.Append(Environment.NewLine);
                foreach (TaskTrace taskTrace in TaskTrace)
                {
                    builder.Append($"{taskTrace}");
                    builder.Append(Environment.NewLine);
                }
                return builder.ToString();
            }

            public void AddTask(RuleContainer ruleContainer, RuleEvaluateResult ruleResult)
            {
                TaskTrace.Add(new TaskTrace(ruleContainer, ruleResult));
            }
        }
    }
}
