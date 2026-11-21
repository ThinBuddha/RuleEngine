using Rules.Rules;
using Rules.Model;
using System.Collections.Generic;
using System.Text;
using System;
using Rules.Expression;

namespace Rules.Tasks
{
    /// <summary>
    /// Evaluates forward containers sequentially. Deduction evaluates one query over
    /// their rules, using a temporary combined container when there are multiple.
    /// </summary>
    /// <remarks>
    /// Choose the evaluation type for each evaluation call.
    /// As a "strategical measure", a <see cref="RuleContainer"/> can request to stop the rest of the process. 
    /// If the request is honored, the remaining <see cref="RuleContainer"/>s are skipped.
    /// To prevent confusion with the Threading.Task class, this class is prefixed with a 'T'.
    /// </remarks>
    public class TTask
    {
        public RuleContainers RuleContainers { get; set; }

        public TaskEvaluateResults Evaluate(ValueBag context, EvaluationType evaluationType, TCompareExpression query = null)
        {
            if (evaluationType == EvaluationType.Deduction && RuleContainers.Containers.Count > 1)
            {
                return EvaluateCombinedDeduction(context, query);
            }

            TaskEvaluateResults taskResults = new TaskEvaluateResults();
            RuleEvaluateResult rulesResult = null;
            foreach (RuleContainer container in RuleContainers.Containers)
            {
                rulesResult = container.Evaluate(context, evaluationType, query);
                taskResults.Evaluations.Add(new TaskTrace(container, rulesResult));
                if (rulesResult.RuleGotFired)
                {
                    taskResults.AddTask(container, rulesResult);

                    if (rulesResult.StopConditionValidated)
                    {
                        // We haven't got any logic to weigh the consequences on our level of perception,
                        // so we simply agree, for now...
                        // A FUTURE improvement may be to (also) delegate this request to a Strategy layer
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

        private TaskEvaluateResults EvaluateCombinedDeduction(ValueBag context, TCompareExpression query)
        {
            const string combinedName = "All containers";
            var combined = new RuleContainer { Name = combinedName };
            var origins = new Dictionary<Rule, string>(ReferenceEqualityComparer.Instance);
            foreach (var container in RuleContainers.Containers)
            {
                foreach (var rule in container.Rules)
                {
                    combined.Rules.Add(rule);
                    origins.TryAdd(rule, container.Name);
                }
            }
            try
            {
                var result = new RuleEngine(combined, origins).EvaluateByDeduction(query, context);
                var taskResults = new TaskEvaluateResults { QueryResult = result.ProvenByInduction };
                taskResults.Evaluations.Add(new TaskTrace(combinedName, result));
                if (result.RuleGotFired) taskResults.AddTask(combinedName, result);
                return taskResults;
            }
            finally
            {
                // The combined container only borrows rule instances for this evaluation.
                combined.Rules.Clear();
            }
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
            public readonly string ContainerName;
            public readonly RuleEvaluateResult RuleResult;

            public TaskTrace(RuleContainer ruleContainer, RuleEvaluateResult ruleResult)
            {
                RuleContainer = ruleContainer;
                ContainerName = ruleContainer.Name;
                RuleResult = ruleResult;
            }

            public TaskTrace(string containerName, RuleEvaluateResult ruleResult)
            {
                RuleContainer = new RuleContainer { Name = containerName };
                ContainerName = containerName;
                RuleResult = ruleResult;
            }

            public override string ToString()
            {
                return new StringBuilder()
                  .Append($"RuleContainer \"{ContainerName}\" says:")
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
            // Includes evaluated containers with no fired rules, unlike the legacy trace.
            public readonly List<TaskTrace> Evaluations = new();
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

            public void AddTask(string containerName, RuleEvaluateResult ruleResult)
            {
                TaskTrace.Add(new TaskTrace(containerName, ruleResult));
            }
        }
    }
}
