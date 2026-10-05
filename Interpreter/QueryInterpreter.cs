using Rules.Model;
using Rules.Rules;

namespace Rules.Interpreter
{
    public class QueryInterpreter
    {
        /// <summary>
        /// Evaluate a user query or task request
        /// </summary>
        /// <remarks>
        /// A query is a question or task request on an abstract level, where the answer or task result is derived
        /// using the model (or value bag in case of no model), production rules and user answers.
        /// </remarks>
        /// <param name="query"></param>
        /// <returns></returns>
        public RuleEvaluateResult Evaluate(string query)
        {
            // TODO: what's the type of query?
            // TODO: what rule engine must be instantiated ?

            return new RuleEvaluateResult();
        }

        public bool EvaluateRule(Rule rule, ValueBag context)
        {
            RuleEvaluateResult evalResult = new RuleEvaluateResult();
            bool ruleResult = rule.Evaluate(context);
            if (ruleResult)
            {
                evalResult.AddOrGetRule(rule);
            }
            return ruleResult;
        }
    }
}
