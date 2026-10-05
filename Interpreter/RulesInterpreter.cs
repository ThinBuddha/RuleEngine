using System;
using System.Data;

namespace Rules.Interpreter
{
    public class RulesInterpreter
    {
        public bool EvaluateRule(Rule rule, ValueBag context)
        {
            RuleEvaluateResult evalResult = new RuleEvaluateResult();
            bool ruleResult = rule.Evaluate(context, ref evalResult);

            return ruleResult;
        }

        /// <summary>
        /// Evaluate a user query or task request
        /// </summary>
        /// <remarks>
        /// A query is a question or task request on an abstract level, where the answer or task result is derived
        /// using the model (or value bag in case of no model), production rules and user answers.
        /// </remarks>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool EvaluateQuery(string query)
        {
            // what's the type of query?
            // what rule engine must be instantiated ?

            return false;
        }
    }
}
